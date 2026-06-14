# Synology NAS Deployment

This deployment keeps release files and secrets separate.

Release files can be wiped and recopied on every deployment. The environment file
must live in a persistent folder that is not wiped.

## One-Time NAS Setup

Create a persistent environment folder on the NAS:

```bash
mkdir -p /volume1/docker/aiterate-energy/env
chmod 700 /volume1/docker/aiterate-energy/env
```

If those commands fail with a permission error, run this once from an SSH session
on the NAS:

```bash
sudo mkdir -p /volume1/docker/aiterate-energy/env /volume1/docker/aiterate-energy/release
sudo chown -R <nas-user>:users /volume1/docker/aiterate-energy
chmod 700 /volume1/docker/aiterate-energy/env
```

Create this file on the NAS:

```text
/volume1/docker/aiterate-energy/env/production.env
```

Use `deploy/nas/production.env.example` as the template, then replace every
placeholder with the real production values. Do not copy the real file back into
Git.

## Per-Release Deployment From Mac

From the repository root on your Mac, run:

```bash
NAS_USER=<nas-user> \
NAS_HOST=<nas-host> \
APP_CONTAINER_NAME=<container-name> \
PUBLIC_URL=<public-url> \
./deploy-to-nas.example.sh
```

The script copies the release files to the NAS, wipes the release folder first,
and runs Docker Compose for you. The compose file always reads:

```text
/volume1/docker/aiterate-energy/env/production.env
```

So redeploying new files does not require entering environment variables again.

## Required Environment Variables

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:3000
ConnectionStrings__Identity=Server=<db-host>;Port=3306;Database=<db>;User=<app-user>;Password=<password>;
MariaDb__ServerVersion=10.11.0
HW_IP=<homewizard-ip>
HW_SCHEME=wss
HomeWizard__CertificateSha256=<homewizard-cert-sha256>
```

Use a dedicated MariaDB application user, not `root`.
