# GIS PhotoShare

GIS PhotoShare is a location-aware photograph sharing platform built around Port Harcourt,
Rivers State, Nigeria. Users upload photographs with real-world coordinates, explore them through an
interactive Leaflet map, and manage a personal gallery. The application is an enhancement of an
existing ASP.NET Core 8 / Vue 2 system; existing accounts, photograph records and uploaded files are
preserved.

## Highlights

- **Port Harcourt by default.** The map opens at approximately 4.8156° N, 7.0498° E, zoom 12. A
  distant photograph never causes the initial view to jump to a global extent.
- **Four coordinate methods.** Use browser geolocation, pick a point on the upload map, enter
  latitude/longitude manually, or apply GPS metadata detected in a JPEG.
- **Interactive photo map.** Marker clustering, layer selection (street, terrain, satellite),
  fullscreen, scale bar, locate-me, pointer coordinates, fit-to-photos, return-to-Port-Harcourt,
  search, category/date filters, viewport search, results panel and GeoJSON export.
- **Secure uploads.** Files are decoded and validated server-side, orientation-corrected,
  metadata-stripped, size-limited, re-encoded where appropriate, thumbnailed, and given
  collision-free names.
- **Ownership and visibility.** Only the owner can edit or delete a photograph. Photographs can be
  public or private; private records and media are excluded from public feeds and exports.
- **Modernised interface.** Responsive map, upload, gallery, detail and edit pages; accessible
  labels, focus states, notifications and mobile navigation; an application-focused footer with no
  personal contact details.
- **Hardened API.** Server-enforced `mine` filtering, pagination, consistent problem responses,
  login rate limiting, account lockout, production JWT-key validation and security headers.

## Technology stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, MediatR 12, FluentValidation 11, explicit DTO projections |
| Persistence | EF Core 8.0.32, SQLite by default, SQL Server optional, InMemory for isolated tests |
| Identity | ASP.NET Core Identity + JWT bearer, lockout and rate limiting |
| Images | SixLabors.ImageSharp 4 image decoding, orientation, resizing and thumbnails |
| Frontend | Vue 2.6, Vue Router 3, Vuex 3, Bootstrap 4 CSS, Leaflet 1.9, MarkerCluster, exifr |
| Build | .NET SDK 8+ (tested with SDK 10 building `net8.0`), Node 18+ and npm |
| Tests | NUnit 4, FluentAssertions, Moq, Respawn, Playwright, Mocha/Chai |

## Architecture

```
Vue single-page client (src/WebUI/clientapp)
  -> /api/auth, /api/photos, /api/upload, /api/settings, /api/geocoding
      -> ASP.NET Core controllers (src/WebUI)
          -> Application layer (MediatR commands/queries, DTOs, validation)
              -> Domain (Photo and template entities)
              -> Infrastructure (EF Core, Identity, image processing, storage, geocoding)
```

The WebUI project builds the Vue client automatically during `dotnet build` and serves the output
from `clientapp/dist`. Uploaded public media is stored under `wwwroot/uploads` by default; private
media is stored outside the web root and served through an authorised endpoint.

## Repository layout

```
src/Domain                    Photo entity and shared domain types
src/Application               Photo/auth commands, queries, DTOs, validation
src/Infrastructure            EF Core, Identity, migrations, file storage, EXIF, geocoding
src/WebUI                     ASP.NET Core host, controllers, middleware, Vue client
tests/Domain.UnitTests        Domain tests
tests/Application.UnitTests   Query, command, validation, EXIF and media-token tests
tests/Applicaton.IntegrationTests  SQL Server LocalDB and SQLite compatibility tests
src/WebUI/clientapp/tests/e2e      Playwright release-readiness suite
docs/audits                   Existing-system audit
docs/implementation           Enhancement summary
docs/testing                  Verification report
docs/deployment               Deployment guide
```

## Prerequisites

- .NET SDK 8 or later (`dotnet --list-sdks`)
- Node.js 18 or later and npm (`node --version`)
- No database server is required for the default SQLite mode
- Optional: SQL Server LocalDB to run the full integration test project
- Optional: Google Chrome or Microsoft Edge for Playwright browser tests

## Quick start

From the repository root:

```powershell
cd src\WebUI\clientapp
npm ci
cd ..\..\..

dotnet build gis-photo-sharing-app.sln
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\WebUI --launch-profile WebUI
```

Open <http://localhost:5000/> (the launch profile also exposes HTTPS on port 5001).

`dotnet build` runs `npm run build` in `clientapp`. Set `/p:SkipVueBuild=true` only when
`clientapp/dist` has already been built.

### Development demo account

When `ASPNETCORE_ENVIRONMENT=Development`, `appsettings.Development.json` enables:

```json
"DatabaseSeed": {
  "DefaultUser": true,
  "SamplePhotos": true
}
```

The development account is:

| Email | Password |
|---|---|
| `administrator@localhost` | `Administrator1!` |

Three demonstration photographs around Port Harcourt are seeded only into an empty database. These
development seeds are disabled by default in `appsettings.json`; production must create its own
administrator through a separate operational process. See the deployment guide.

### Production warning

Outside Development the application refuses to start unless `Jwt:Key` is a unique, high-entropy value
of at least 32 bytes. Do not ship the development key. Example generation:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

## Configuration

Configuration is read from `appsettings.json`, `appsettings.{Environment}.json`, environment
variables and command-line arguments. Environment variables use `__` for nesting:

| Setting | Purpose |
|---|---|
| `DatabaseProvider` | `Sqlite` (default) or `SqlServer` |
| `UseInMemoryDatabase` | `true` only for isolated test/demo runs |
| `ConnectionStrings:SqliteConnection` | SQLite connection string; relative paths resolve under `src/WebUI/App_Data` |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `DatabaseSeed:DefaultUser` | Create the development administrator when true |
| `DatabaseSeed:SamplePhotos` | Seed the three development sample photographs when true |
| `MapSettings` | Default latitude/longitude/zoom, zoom limits, location name and tile layers |
| `PhotoSettings` | Upload limits, pixel/dimension limits, thumbnail size, categories, allowed content types and optional `StorageRoot` |
| `Geocoding` | Optional Nominatim reverse geocoding, attribution, user agent and rate interval |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | JWT signing and validation |
| `Https:RedirectEnabled` | Set false for local HTTP-only development; true in production |
| `ReverseProxy:Enabled` / `KnownProxies` | Forwarded-header handling behind a trusted reverse proxy |

Defaults for the map view:

```json
"MapSettings": {
  "DefaultLatitude": 4.8156,
  "DefaultLongitude": 7.0498,
  "DefaultZoom": 12,
  "MinimumZoom": 3,
  "MaximumZoom": 19,
  "DefaultLocationName": "Port Harcourt, Rivers State, Nigeria"
}
```

The map opens with OpenStreetMap standard tiles. OpenTopoMap and Esri World Imagery are available in
the layer control. All layers carry visible attribution. Reverse geocoding is optional; uploads never
depend on it.

### File storage

By default:

- Public images: `src/WebUI/wwwroot/uploads/photos`
- Public thumbnails: `src/WebUI/wwwroot/uploads/thumbnails`
- Private images: `src/WebUI/App_Data/private-uploads`
- Private thumbnails: `src/WebUI/App_Data/private-uploads/thumbnails`

Set `PhotoSettings:StorageRoot` to an absolute path (or a path relative to the content root) for a
single storage root. The application writes public files to `{StorageRoot}/public` and private files
to `{StorageRoot}/private`, while retaining read fallback to legacy `wwwroot/uploads` files.

## Database

### SQLite (default)

On startup the application:

1. Creates the SQLite schema with `EnsureCreated` for a new database.
2. Runs the idempotent `SqliteSchemaUpgrader` to add missing columns and indexes to existing
   databases without deleting rows.
3. Applies only the configured seed operations.

`EnsureCreated` does not apply EF migrations, so the upgrader is the supported compatibility path for
existing SQLite files. It has been tested against a legacy photo table and preserves existing records,
file paths and ownership.

To reset a development database, stop the application and move or delete `src/WebUI/App_Data`. Do not
delete `wwwroot/uploads` unless you also intend to remove stored media.

### SQL Server (optional)

1. Set `DatabaseProvider` to `SqlServer`.
2. Set `ConnectionStrings:DefaultConnection`.
3. Apply migrations:

```powershell
dotnet ef database update --project src\Infrastructure --startup-project src\WebUI
```

The `AddPhotoMetadataAndIndexes` migration adds `LocationName`, `Category`, `IsPublic` (default true)
and indexes. Existing rows remain public and are assigned the `General` category. Rollback guidance
is in [the deployment guide](docs/deployment/gis-photoshare-deployment-guide.md).

## API overview

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/auth/register` | Register and receive a JWT |
| `POST` | `/api/auth/login` | Sign in (rate-limited, lockout-aware) |
| `GET` | `/api/settings` | Public map, photo and geocoding configuration |
| `GET` | `/api/photos` | Public/accessible photo feed with `mine`, search, category, date and bounding-box filters |
| `GET` | `/api/photos/gallery` | Authenticated paginated gallery |
| `GET` | `/api/photos/{id}` | Photo detail (private records require the owner) |
| `GET` | `/api/photos/{id}/file` | Authorised media; private images require owner auth or a short-lived token |
| `GET` | `/api/photos/{id}/thumbnail` | Authorised thumbnail |
| `GET` | `/api/photos/geojson` | Public GeoJSON export with `[longitude, latitude]` positions |
| `POST` | `/api/upload/photo` | Authenticated multipart upload |
| `POST` | `/api/upload/exif` | Inspect GPS/date metadata in an image |
| `PUT` | `/api/photos/{id}` | Owner-only metadata/coordinate update |
| `DELETE` | `/api/photos/{id}` | Owner-only delete |
| `GET` | `/api/geocoding/reverse` | Authenticated optional reverse-geocoding proxy |
| `GET` | `/health` | Liveness check |

Input validation failures return `400` with a ProblemDetails-shaped body. Unauthorised, forbidden and
missing resources return `401`, `403` and `404` respectively. Unknown `/api` routes return a JSON
`404` rather than the SPA shell. Stack traces are not returned to clients.

## Testing

```powershell
# Backend tests (domain, application, integration)
dotnet test gis-photo-sharing-app.sln

# Vue coordinate utilities
cd src\WebUI\clientapp
npm run test:unit

# Browser release-readiness suite (Playwright, uses Chrome/Chromium and isolated in-memory data)
npm run test:e2e
```

Verified counts at the time of handover:

- Domain tests: **5 passed**
- Application unit tests: **34 passed**
- Integration tests: **30 passed**
- Vue unit tests: **8 passing**
- Playwright end-to-end tests: **10 passed**

The integration project uses `gis_photo_sharing_appTestDb` in SQL Server LocalDB and a separate
temporary SQLite database for compatibility/persistence tests. Playwright uses an in-memory database
and isolated storage under `artifacts/e2e-storage`, so it does not alter the development database.

See the [verification report](docs/testing/gis-photoshare-verification-report.md) for exact commands,
runtime checks and limitations.

## Documentation

- [Existing-system audit](docs/audits/gis-photoshare-existing-system-audit.md)
- [Enhancement summary](docs/implementation/gis-photoshare-enhancement-summary.md)
- [Verification report](docs/testing/gis-photoshare-verification-report.md)
- [Deployment guide](docs/deployment/gis-photoshare-deployment-guide.md)
- [Database notes](database/README.md)
- [Demonstration guide](DEMO_GUIDE.md)

## Known limitations

- SQLite is a single-file database and is not intended for multi-instance production hosting without
  a shared database such as SQL Server.
- The file storage backend is local disk. A distributed deployment needs shared storage or a storage
  adapter before scaling horizontally.
- Account self-service deletion is not implemented; an operator must provide a deletion process.
- JWT access tokens last seven days, are stored in browser local storage, and are not individually
  revocable. Sign-out removes the browser copy. Use a short deployment-specific token lifetime and a
  strict XSS/CSP posture if your risk model requires it.
- Email confirmation and password reset are not implemented in this academic/demo build.
- The upload multipart request ceiling is fixed at approximately 10.5 MB. `PhotoSettings:MaxUploadBytes`
  must remain at or below the 10 MB file limit unless the controller limit is adjusted in code.
- Reverse geocoding depends on the external Nominatim service and its usage policy. Uploads do not
  require it.
- Map tiles depend on external providers and require network access. Tile failures are reported in
  the interface without blocking access to photo metadata.
- ImageSharp 4 is distributed under the Six Labors Split License; review dependency licensing before
  commercial deployment.
- The client uses Vue 2 (end-of-life since December 2023). `npm audit --omit=dev` reports one low
  severity Vue 2 ReDoS advisory whose fix requires a Vue 3 migration. The migration was not performed
  because it would be a major frontend replacement; plan it as future work.
- Automated browser tests cover desktop and mobile viewports in Chromium. Safari and Firefox were not
  exercised in this environment.
- No Content-Security-Policy is configured because Leaflet and the map tiles require careful policy
  testing. Add and validate a suitable CSP before a high-assurance deployment.
- No CI pipeline is configured; the commands above are suitable for a CI job.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `clientapp\dist\index.html` missing | Run `npm ci` and `npm run build` in `src\WebUI\clientapp`, then rebuild. |
| Blank page at `/` | Confirm `clientapp/dist` exists and the WebUI is running from `src\WebUI`. |
| `401` on upload | Send `Authorization: Bearer <token>` from `/api/auth/login`. |
| Uploaded image not shown | Check the configured storage root and the media endpoint; private media requires owner auth. |
| Geocoding unavailable | Leave the location name blank or enter it manually; uploads still succeed. |
| Geolocation denied | Use HTTPS or localhost, or pick a point on the map / enter coordinates manually. |
| Production startup fails on JWT | Set a unique `Jwt:Key` of at least 32 bytes with sufficient character diversity. |
| Integration tests fail with LocalDB errors | Install SQL Server Express LocalDB or run the unit/SQLite tests only. |
| Playwright says browser missing | Run `npx playwright install chromium` or configure a system browser channel. |
| Old Vue CLI/OpenSSL error | Use the npm scripts; they set `NODE_OPTIONS=--openssl-legacy-provider`. |
