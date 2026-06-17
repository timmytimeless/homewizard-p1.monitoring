# Project Notes For Codex

This repository is an ASP.NET Core MVC app for HomeWizard P1 monitoring.
It is both a small demo and a practical deployment example for limited-resource
hosting on a Synology NAS with Docker.

## Current Purpose

- Stream HomeWizard P1 measurements in real time through the web app.
- Store compact 15-minute MariaDB aggregates instead of raw minute-level rows.
- Demonstrate a setup that runs on modest hardware, including a stock
  Synology DS925+ with 4 GB RAM.

## Current Architecture

- Web project: `aiterate.energy.web`
- Framework: .NET 9 / ASP.NET Core MVC
- Persistence: MariaDB via EF Core + Pomelo
- Identity: ASP.NET Identity in the same MariaDB database
- Runtime token protection: ASP.NET Data Protection
- Realtime source: HomeWizard local API `/api/ws` for live streaming
- Background collector: hosted service in the web app that polls
  `/api/measurement` and writes 15-minute aggregates

## Important Runtime Facts

- Local development currently uses the `AiterateDev` database.
- Production should use a separate database and a persistent
  `DataProtection__KeysPath`.
- The HomeWizard token is stored encrypted in `AspNetUsers.HomeWizardP1Token`.
- If Data Protection keys change or are lost, existing encrypted tokens will
  become unreadable and must be saved again.
- The collector is enabled in `appsettings.Development.json` and disabled in
  `appsettings.json` by default.

## Data Model And Flow

- `Models/HomeWizardMeasurement.cs` is the HomeWizard API DTO.
- `Models/Persistence/HomeWizardQuarterHourAggregate.cs` is the MariaDB entity
  for the 15-minute rollup table.
- The collector calculates:
  - kWh deltas from cumulative counters
  - power min/max/average
  - voltage/current averages
  - counter deltas for sag/swell/power-fail counts
- The collector tries to decrypt stored user tokens and skips tokens it cannot
  decrypt in the current app instance.

## Key Files

- `aiterate.energy.web/Program.cs`
- `aiterate.energy.web/Controllers/HomeController.cs`
- `aiterate.energy.web/Controllers/AccountController.cs`
- `aiterate.energy.web/Data/ApplicationDbContext.cs`
- `aiterate.energy.web/Services/HomeWizard/HomeWizardMeasurementCollectorService.cs`
- `aiterate.energy.web/Services/HomeWizard/HomeWizardAggregateUpdater.cs`
- `aiterate.energy.web/Services/HomeWizard/HomeWizardCertificateValidator.cs`

## Local Configuration

- User secrets currently hold the local MariaDB connection string.
- The local connection string points to `AiterateDev`.
- `HomeWizardCollector:Enabled` is set to `true` in development config.
- `HomeWizardCollector:Scheme` is `https`.
- `HomeWizardCollector:Host` is the HomeWizard device IP.
- `HomeWizard:CertificateSha256` pins the device certificate.
- `DataProtection:KeysPath` in development points to `.data-protection-keys`.

## Deployment Notes

- Do not use MariaDB `root` for the application connection.
- The NAS deployment expects a persistent env directory and a persistent
  data-protection key directory.
- Production should terminate HTTPS at the reverse proxy and forward
  `X-Forwarded-Proto`, `X-Forwarded-For`, and `X-Forwarded-Host`.
- Keep the container on internal HTTP port `3000` behind the proxy.

## Repository State To Watch

- Ignored local artifacts exist and should stay untracked:
  - `.idea/`
  - `aiterate.energy.web/.data-protection-keys/`
  - `bin/`
  - `obj/`
- `appsettings*.json`, deployment examples, and the collector code have been
  updated to reflect the current HomeWizard aggregation setup.

## Working Guidance For Future Codex Sessions

- Prefer keeping changes narrow and aligned with the existing MVC + EF style.
- Check `Program.cs` and the HomeWizard service files first when touching data
  collection or deployment behavior.
- If the token stops decrypting, verify the active Data Protection keys before
  assuming the user token itself is wrong.
- If aggregates are not being written, inspect the collector logs, the current
  `HomeWizardCollector` settings, and the `AiterateDev` database table
  `HomeWizardQuarterHourAggregates`.
