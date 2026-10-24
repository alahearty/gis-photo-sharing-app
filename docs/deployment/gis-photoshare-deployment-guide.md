# GIS PhotoShare — Deployment Guide

This guide describes a production-oriented deployment of the existing ASP.NET Core 8 application. It
does not change repository deployment settings automatically. Validate each step in your target
environment before going live.

## 1. Prerequisites

- .NET 8 runtime/ASP.NET Core runtime (or the .NET 8 SDK if building on the server)
- Node.js 18+ and npm only for building the Vue client
- A writable data directory for SQLite or a SQL Server database
- A writable media storage directory
- An HTTPS certificate and reverse proxy or direct Kestrel HTTPS binding
- Optional: SQL Server for a multi-user or multi-instance deployment

## 2. Build and publish

From the repository root:

```powershell
cd src\WebUI\clientapp
npm ci
cd ..\..\..

dotnet publish src\WebUI\WebUI.csproj -c Release -o publish
```

The WebUI project runs the Vue production build automatically during publish. `clientapp/dist` must
exist; do not use `/p:SkipVueBuild=true` unless you have already built and verified it.

The publish output contains the API, the Vue client, and the EF Core migrations. The publish process
does not copy the development SQLite database.

## 3. Environment configuration

Set `ASPNETCORE_ENVIRONMENT=Production` (or another non-Development name). This disables Swagger,
HTTPS-redirect override, development seeds and the development exception behaviour.

Minimum production variables:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5000'
$env:DatabaseProvider = 'Sqlite'                 # or SqlServer
$env:ConnectionStrings__SqliteConnection = 'Data Source=/var/lib/gis-photoshare/gis_photo_sharing_app.db'
$env:Jwt__Key = '<48 random bytes, base64>'
$env:Jwt__Issuer = 'gis-photo-sharing-app'
$env:Jwt__Audience = 'gis-photo-sharing-app'
$env:Https__RedirectEnabled = 'false'            # true when TLS terminates at Kestrel; false behind a trusted HTTPS proxy
```

Generate a signing key:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Outside Development the application **fails to start** if `Jwt:Key` is missing, too short, predictable
or low-diversity. The default development key in the repository must never be used in production.

### Seeding

Production seeding is off by default:

```json
"DatabaseSeed": {
  "DefaultUser": false,
  "SamplePhotos": false
}
```

Do not enable the development administrator or sample photographs in production. Provision the first
administrator through an operational process appropriate to your organisation, or leave registration
open only if that is an intentional policy.

### Reverse proxy

If TLS terminates at nginx, IIS or another trusted proxy, enable forwarded headers:

```json
"ReverseProxy": {
  "Enabled": true,
  "KnownProxies": [ "127.0.0.1", "10.0.0.10" ]
}
```

Only list proxies you control. `ForwardLimit` is 1. Set `Https:RedirectEnabled` appropriately for the
proxy topology and ensure the proxy sends `X-Forwarded-Proto`.

## 4. Database

### SQLite

Create the data directory and give the application account read/write access:

```powershell
New-Item -ItemType Directory -Force C:\ProgramData\GISPhotoShare
# Grant the service account Modify permission on this directory.
```

Use an absolute, non-web-accessible path. Back up the database and its `-wal`/`-shm` files while the
application is stopped or use SQLite’s online backup API.

On startup the application creates a new schema with `EnsureCreated` and runs the idempotent
`SqliteSchemaUpgrader` for existing files. Test the upgrade against a copy of the production database
before the real deployment.

SQLite is appropriate for a single application instance. It is not a multi-writer production database.

### SQL Server

Set `DatabaseProvider=SqlServer` and `ConnectionStrings__DefaultConnection`. Apply migrations with a
deployment identity that has schema-change permission:

```powershell
dotnet ef database update --project src\Infrastructure --startup-project src\WebUI
```

At minimum, apply the `AddPhotoMetadataAndIndexes` migration. Verify existing rows are still public and
that file paths and ownership are unchanged.

### Migration rollback

Take a verified backup before applying changes. The SQL Server `Down` migration drops the added columns
and indexes; that destroys location names, categories and visibility values. It does not remove media
files. Restore the backup if a rollback is required. SQLite rollback is by restoring the pre-upgrade
file. See [database notes](../../database/README.md).

## 5. Media storage

Default locations:

- `wwwroot/uploads/photos` — public images
- `wwwroot/uploads/thumbnails` — public thumbnails
- `App_Data/private-uploads` — private images and thumbnails

For production, prefer a dedicated persistent volume outside the deployment directory:

```powershell
$env:PhotoSettings__StorageRoot = 'D:\GISPhotoShareData\media'
```

The application writes public media to `{StorageRoot}/public` and private media to
`{StorageRoot}/private`. Existing files under `wwwroot/uploads` are read as a fallback.

Requirements:

- The application identity needs create/read/delete permission on the storage root.
- Do not serve the storage root directly from the web server; media is delivered through the authorised
  `/api/photos/{id}/file` endpoint so private access can be checked.
- Back up public and private media together with the database so records and files remain consistent.
- Plan a retention policy and monitor disk capacity.
- Keep request limits aligned with `PhotoSettings:MaxUploadBytes`. The default multipart ceiling is
  approximately 10.5 MB.

## 6. HTTPS and headers

- Terminate TLS at a trusted reverse proxy or configure Kestrel HTTPS.
- Redirect HTTP to HTTPS in production (`Https:RedirectEnabled=true`) unless all traffic is guaranteed
  HTTPS by the proxy.
- The application emits `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
  `Permissions-Policy` and `Cross-Origin-Opener-Policy`.
- HSTS is enabled outside Development. Only enable HSTS permanently once HTTPS is fully operational.
- Browser geolocation requires a secure context: HTTPS or localhost.
- Do not expose the Swagger UI in production; it is only mapped in Development.
- No Content-Security-Policy is set by default. If you add one, account for Leaflet’s inline styles,
  local scripts/styles, the configured tile hosts, and blob/data image previews. Test map layers and
  the upload preview before enforcing it.
- Set a meaningful `Geocoding:UserAgent` that identifies the deployment and, where possible, a contact
  URL, consistent with the public Nominatim usage policy.

## 7. Map and geocoding services

- OpenStreetMap, OpenTopoMap and Esri World Imagery are requested by the browser directly. Review each
  provider’s tile usage policy and attribution requirements for expected traffic.
- No map API key is required by the default configuration. If you add a keyed provider, keep the key on
  the server or in the appropriate restricted frontend configuration; never commit it.
- Reverse geocoding uses the configured Nominatim endpoint. Set `Geocoding:Enabled=false` if outbound
  access is not permitted. The application rate-limits requests and uploads work without geocoding.
- Respect external service terms, attribution and privacy obligations. The application sends selected
  coordinates only when the user explicitly requests a place lookup.

## 8. Run the application

Direct Kestrel example:

```powershell
dotnet .\publish\gis_photo_sharing_app.WebUI.dll --urls "http://127.0.0.1:5000"
```

Windows Service/IIS:

- Publish the application.
- Configure the service/IIS app pool identity.
- Grant that identity access to the database and media directories.
- Set environment variables in the service/host configuration, not in source control.
- Set the ASP.NET Core hosting bundle and correct application pool bitness.
- Do not point IIS directly at the publish folder if private media or the SQLite database is inside it;
  use a separate data root and a reverse-proxy/app-service arrangement.

Check:

```powershell
Invoke-WebRequest http://127.0.0.1:5000/health
```

## 9. Production checklist

Before go-live:

- [ ] Unique `Jwt:Key` configured and stored securely.
- [ ] `ASPNETCORE_ENVIRONMENT` set to a non-Development value.
- [ ] Development seeds disabled.
- [ ] HTTPS enforced and certificate valid.
- [ ] HSTS enabled only after HTTPS validation.
- [ ] Database upgraded from a tested backup.
- [ ] Existing photo records and media verified after migration.
- [ ] Database and media backups scheduled and restore-tested.
- [ ] Storage permissions restricted to the application identity.
- [ ] Reverse proxy `KnownProxies` restricted.
- [ ] External map/geocoding terms reviewed.
- [ ] Privacy and terms pages reviewed and approved by the deployment owner, with a valid contact route.
- [ ] Account deletion and support processes defined.
- [ ] Logs monitored; no secrets or token query strings are written to shared logs.
- [ ] `.NET`, npm and dependency vulnerability scans run in CI.
- [ ] Load test completed for the expected photo count and upload volume.
- [ ] Rate limits and upload ceilings match the expected usage.

## 10. Operational notes and limitations

- Multi-instance deployments require SQL Server (or another shared database) and shared media storage.
  Local SQLite/local disk is instance-local.
- Private media URLs contain short-lived tokens (30 minutes). Do not log or share those URLs.
- Account self-service deletion, password reset and email confirmation are not implemented.
- The legacy `/api/photos` create endpoint is restricted to public `/uploads/` paths; use
  `/api/upload/photo` for new uploads.
- The Todo endpoints remain from the original template and are not part of the public UI.
- No CI pipeline is included; add the documented build/test commands to your pipeline.
- ImageSharp 4 is under the Six Labors Split License; review licensing for commercial use.
- Vue 2 is end-of-life. A low-severity ReDoS advisory remains in the Vue 2 runtime; fixing it requires
  a Vue 3 migration, which is a major frontend change and is recommended as future work.
- Production monitoring should cover `/health`, database size, media disk usage, 5xx rates,
  authentication rate-limit rejection rates and tile/geocoding failures.
