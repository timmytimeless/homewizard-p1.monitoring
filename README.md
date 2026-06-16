# energy-monitoring

## Configuration

The MariaDB Identity connection string is read from the `ConnectionStrings__Identity`
environment variable. Do not commit real database credentials.

Local shell example:

```bash
export ConnectionStrings__Identity='Server=<mariadb-host>;Port=3306;Database=<database>;User=<app-user>;Password=<db-password>;'
export MariaDb__ServerVersion='10.11.0'
dotnet run --project aiterate.energy.web/aiterate.energy.web.csproj
```

Apply migrations with the same environment variable set:

```bash
./.tools/dotnet-ef database update \
  --project aiterate.energy.web/aiterate.energy.web.csproj \
  --startup-project aiterate.energy.web/aiterate.energy.web.csproj \
  --context ApplicationDbContext
```

## Synology Docker Deployment Example

The repeatable NAS deployment example uses `docker-compose.example.yml`,
`deploy-to-nas.example.sh`, and an environment file stored outside the release
folder. Copy the example files to local ignored files for your own deployment.

One-time setup on the NAS:

```bash
mkdir -p /volume1/docker/aiterate-energy/env
chmod 700 /volume1/docker/aiterate-energy/env
```

If Synology reports a permission error, SSH into the NAS and run this once:

```bash
sudo mkdir -p /volume1/docker/aiterate-energy/env /volume1/docker/aiterate-energy/release
sudo chown -R <nas-user>:users /volume1/docker/aiterate-energy
chmod 700 /volume1/docker/aiterate-energy/env
```

Create this persistent file on the NAS:

```text
/volume1/docker/aiterate-energy/env/production.env
```

Use `deploy/nas/production.env.example` as the template:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:3000
DataProtection__KeysPath=/var/aiterate-energy/data-protection-keys
ConnectionStrings__Identity=Server=<nas-or-db-host>;Port=3306;Database=<database>;User=<app-user>;Password=<password>;
MariaDb__ServerVersion=10.11.0
HW_IP=<homewizard-ip>
HW_SCHEME=wss
HomeWizard__CertificateSha256=<sha256-fingerprint>
```

Use a dedicated MariaDB user with only the privileges this app needs for its
database. Do not use `root` for the application connection.

Keep `DataProtection__KeysPath` on the mounted persistent key directory. Without
those key files, encrypted values such as the HomeWizard P1 token cannot be
decrypted after a container restart or redeploy.

For each redeployment from your Mac, set your own deployment values and run:

```bash
NAS_USER=<nas-user> \
NAS_HOST=<nas-host> \
APP_CONTAINER_NAME=<container-name> \
PUBLIC_URL=https://energy.aiterate.nl \
./deploy-to-nas.example.sh
```

The script wipes and refreshes the NAS release folder, then runs Docker Compose
on the NAS. The real environment file is not touched because it lives in:

```text
/volume1/docker/aiterate-energy/env/production.env
```

For HomeWizard devices with a certificate that cannot be validated by hostname,
set `HomeWizard__CertificateSha256` to the device certificate fingerprint. This
pins the WebSocket TLS connection to that exact certificate instead of accepting
all certificates.

For production HTTPS, keep the container on internal HTTP port `3000` and
terminate TLS in Synology's reverse proxy:

```text
https://energy.aiterate.nl -> http://127.0.0.1:3000
```

The app reads the proxy's forwarded headers in production so secure cookies,
HSTS, and HTTPS request detection work correctly.
