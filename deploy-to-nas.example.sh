#!/usr/bin/env bash
set -euo pipefail

: "${NAS_USER:?Set NAS_USER, for example: NAS_USER=myuser}"
: "${NAS_HOST:?Set NAS_HOST, for example: NAS_HOST=192.0.2.10}"

NAS_ROOT="${NAS_ROOT:-/volume1/docker/aiterate-energy}"
NAS_RELEASE_DIR="$NAS_ROOT/release"
NAS_ENV_FILE="$NAS_ROOT/env/production.env"
NAS_DATA_PROTECTION_KEYS_DIR="$NAS_ROOT/data-protection-keys"
APP_CONTAINER_NAME="${APP_CONTAINER_NAME:-aiterate-energy-web}"
PUBLIC_URL="${PUBLIC_URL:-http://localhost:3000}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.example.yml}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

if [ ! -f "$COMPOSE_FILE" ]; then
  echo "ERROR: $COMPOSE_FILE was not found. Run this script from the repository root."
  exit 1
fi

echo "Deploying aiterate.energy to $NAS_USER@$NAS_HOST"
echo "Release folder: $NAS_RELEASE_DIR"
echo "Environment file: $NAS_ENV_FILE"
echo "Data Protection keys folder: $NAS_DATA_PROTECTION_KEYS_DIR"
echo "Container name: $APP_CONTAINER_NAME"
echo "Compose file: $COMPOSE_FILE"
echo

echo "Checking NAS folders and environment file..."
ssh "$NAS_USER@$NAS_HOST" "
  set -e
  mkdir -p '$NAS_RELEASE_DIR'
  mkdir -p '$NAS_DATA_PROTECTION_KEYS_DIR'
  if [ ! -f '$NAS_ENV_FILE' ]; then
    echo 'ERROR: Missing environment file: $NAS_ENV_FILE'
    echo 'Create it once using deploy/nas/production.env.example as the template.'
    exit 2
  fi
"

echo "Uploading release files..."
COPYFILE_DISABLE=1 tar \
  --exclude='./.git' \
  --exclude='./bin' \
  --exclude='./obj' \
  --exclude='./.env' \
  --exclude='./.env.*' \
  --exclude='./.idea' \
  --exclude='./.tools' \
  --exclude='./*.local.*' \
  --exclude='./aiterate.energy.web/bin' \
  --exclude='./aiterate.energy.web/obj' \
  --exclude='./*.crt' \
  -czf - . | ssh "$NAS_USER@$NAS_HOST" "
    set -e
    rm -rf '$NAS_RELEASE_DIR'/*
    tar -xzf - -C '$NAS_RELEASE_DIR'
  "

echo "Rebuilding and restarting Docker container..."
ssh -t "$NAS_USER@$NAS_HOST" "
  set -e
  PATH=\"/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin:/var/packages/ContainerManager/target/usr/bin:\$PATH\"
  DOCKER_BIN=\$(command -v docker || true)
  if [ -z \"\$DOCKER_BIN\" ]; then
    echo 'ERROR: docker was not found in this SSH session.'
    exit 3
  fi

  docker_cmd() {
    \"\$DOCKER_BIN\" \"\$@\"
  }

  if ! \"\$DOCKER_BIN\" info >/dev/null 2>&1; then
    if ! command -v sudo >/dev/null 2>&1; then
      echo 'ERROR: Docker requires elevated permissions, but sudo was not found.'
      exit 4
    fi

    docker_cmd() {
      sudo \"\$DOCKER_BIN\" \"\$@\"
    }
  fi

  cd '$NAS_RELEASE_DIR'
  export APP_CONTAINER_NAME='$APP_CONTAINER_NAME'
  export AITERATE_ENV_FILE='$NAS_ENV_FILE'
  export AITERATE_DATA_PROTECTION_KEYS_DIR='$NAS_DATA_PROTECTION_KEYS_DIR'

  docker_cmd compose -f '$COMPOSE_FILE' down --remove-orphans

  if docker_cmd container inspect '$APP_CONTAINER_NAME' >/dev/null 2>&1; then
    echo 'Removing existing $APP_CONTAINER_NAME container so compose can recreate it...'
    docker_cmd rm -f '$APP_CONTAINER_NAME'
  fi

  docker_cmd compose -f '$COMPOSE_FILE' up -d --build
  docker_cmd image prune -f
"

echo
echo "Deployment complete."
echo "Open: $PUBLIC_URL"
