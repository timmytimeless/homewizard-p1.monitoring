# Synology NAS Deployment

This deployment keeps release files and secrets separate.

Release files can be wiped and recopied on every deployment. The environment file
must live in a persistent folder that is not wiped.

## One-Time NAS Setup

Create persistent environment and Data Protection key folders on the NAS:

```bash
mkdir -p /volume1/docker/aiterate-energy/env /volume1/docker/aiterate-energy/data-protection-keys
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
DataProtection__KeysPath=/var/aiterate-energy/data-protection-keys
ConnectionStrings__Identity=Server=<db-host>;Port=3306;Database=<db>;User=<app-user>;Password=<password>;
ConnectionStrings__Migrations=Server=<db-host>;Port=3306;Database=<db>;User=aiterate_migrator;Password=<migrator-password>;
MariaDb__ServerVersion=10.11.0
HomeWizard__Host=<homewizard-ip>
HW_SCHEME=wss
HomeWizard__CertificateSha256=<homewizard-cert-sha256>
HomeWizardCollector__Scheme=https
HomeWizardCollector__Host=<homewizard-ip>
HomeWizardCollector__PollIntervalSeconds=60
HomeWizardCollector__BucketMinutes=15
HomeWizardCollector__Token=<homewizard-p1-token>
EnphaseCollector__Enabled=true
EnphaseCollector__Scheme=https
EnphaseCollector__Host=<enphase-gateway-ip>
EnphaseCollector__Endpoint=/production.json
EnphaseCollector__PollIntervalSeconds=300
EnphaseCollector__BucketMinutes=15
EnphaseCollector__TimeZoneId=Europe/Amsterdam
EnphaseCollector__AllowInvalidCertificate=true
EnphaseCollector__Token=<enphase-gateway-token>
```

Use a dedicated MariaDB application user, not `root`.
Use `ConnectionStrings__Migrations` only for design-time EF migration commands;
the running app should use `ConnectionStrings__Identity` with the least
privileged application user.

`DataProtection__KeysPath` must point to the mounted
`/var/aiterate-energy/data-protection-keys` directory. The files in that
directory are required to decrypt the stored HomeWizard P1 token after a
container restart or redeploy.

## Collector

Docker Compose starts two containers:

- `web`: MVC UI, reporting, and realtime Raw Data/Insights display.
- `collector`: dedicated .NET Worker Service that polls the HomeWizard P1 meter
  and Enphase gateway, then writes `HomeWizardQuarterHourAggregates` and
  `EnphaseQuarterHourAggregates`.

The MVC web app does not register or run the background collector and must not
write P1 measurements or aggregates to MariaDB. Docker Compose enables database
collection only in the collector container. Keep only one collector container
running for a single P1 meter, otherwise multiple processes may try to update
the same 15-minute aggregate row.

Prefer configuring `HomeWizardCollector__Token` in the NAS environment file for
the worker. If it is omitted, the collector can fall back to the token stored in
`AspNetUsers.HomeWizardP1Token`, but then the worker must share the same
Data Protection keys as the web app.

Configure `EnphaseCollector__Token` in the NAS environment file. The Enphase
collector polls the local gateway every 300 seconds by default and writes
15-minute solar production aggregates based on the gateway `whLifetime` delta.
`EnphaseCollector__TimeZoneId` should remain `Europe/Amsterdam` so the gateway
Unix `readingTime` is bucketed in Dutch local time even when the Docker
container itself runs in UTC.

## HTTPS

Keep the Docker container on internal HTTP port `3000` and terminate HTTPS in
Synology's reverse proxy or another proxy in front of Docker.

Configure the proxy to forward:

```text
https://energy.aiterate.nl -> http://127.0.0.1:3000
```

The app trusts the proxy's `X-Forwarded-Proto`, `X-Forwarded-For`, and
`X-Forwarded-Host` headers in production, so generated URLs, secure cookies,
HSTS, and request HTTPS detection work correctly behind the proxy.
