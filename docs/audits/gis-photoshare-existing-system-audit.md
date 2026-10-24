# GIS PhotoShare — Existing System Audit

**Audit date:** 23 October 2026  
**Scope:** The existing solution at `gis-photo-sharing-app.sln`, including the ASP.NET Core API, Vue 2 client, EF Core persistence, file storage, authentication, map rendering and automated tests.  
**Audit method:** Static inspection of the solution and representative runtime checks. This audit was completed before production code was changed.

## 1. Executive summary

GIS PhotoShare is an existing ASP.NET Core 8 / Vue 2 application built on the Clean Architecture
solution template. The core workflow is present: users register, sign in, upload an image with
coordinates, view photos on Leaflet, and edit or delete their own records. The application builds
from a coherent layered structure and already persists data in SQLite by default.

The main gaps are not architectural. They are product and correctness gaps:

- The map is hardcoded to New York and automatically fits all photos, so it cannot satisfy the
  Port Harcourt default-view requirement.
- Coordinates are available through manual entry and browser geolocation only. There is no
  upload-page map picker, no EXIF GPS detection, and no consistent client-side coordinate
  validation/synchronisation.
- The upload endpoint trusts the browser-supplied MIME type, stores files before validation is
  complete, and silently accepts undecodable images.
- Search, filtering, pagination, clustering, gallery management and GeoJSON export are absent.
- The public footer exposes personal contact information and unverified social links.
- The seed process creates a known administrator account and sample New York photographs in every
  environment, including production.
- Existing SQLite databases cannot be upgraded by `EnsureCreated`, so any schema enhancement must
  include a safe upgrade path.

The recommended approach is incremental: retain the Clean Architecture layers, Identity/JWT,
SQLite/SQL Server support, Vue 2 SPA and Leaflet. Extend the existing `Photo` entity and API,
introduce configuration-driven map and photo settings, harden uploads, rebuild the map and upload
experiences on the existing visual identity, and add verification around coordinates, ownership and
visibility.

## 2. Existing architecture

### 2.1 Solution structure

| Project | Role |
|---|---|
| `src/Domain` | `Photo`, `TodoList`, `TodoItem`, auditable base entity, value objects |
| `src/Application` | MediatR commands/queries, DTOs, FluentValidation, pipeline behaviours |
| `src/Infrastructure` | EF Core `ApplicationDbContext`, Identity, JWT setup, local file storage, CSV service |
| `src/WebUI` | ASP.NET Core controllers, Startup/Program, Vue 2 SPA in `clientapp`, uploaded files in `wwwroot` |
| `tests/Domain.UnitTests` | Domain tests |
| `tests/Application.UnitTests` | Mapping and behaviour tests |
| `tests/Applicaton.IntegrationTests` | MediatR integration tests against SQL Server LocalDB |

The project uses ASP.NET Core 8, EF Core 8, MediatR 12, FluentValidation 11, AutoMapper 12,
ASP.NET Core Identity with JWT bearer authentication, and SixLabors.ImageSharp 3.1.12.

### 2.2 Frontend

- Vue 2.6, Vue Router 3, Vuex 3, Bootstrap 4, Font Awesome 4, Leaflet 1.9.
- Vue CLI 4 build, with `npm run build` invoked automatically from `WebUI.csproj`.
- The SPA is served from `clientapp/dist`; uploaded files are served from `wwwroot/uploads`.
- There is no TypeScript, no component library beyond Bootstrap, and no browser automation in the
  repository.

### 2.3 Persistence

- Default runtime provider: SQLite at `src/WebUI/App_Data/gis_photo_sharing_app.db`.
- SQLite schema is created with `Database.EnsureCreatedAsync()`; there are no SQLite migrations.
- SQL Server is supported through the checked-in `InitialCreate` migration.
- `Photo` currently stores `Latitude` and `Longitude` as `double` (no spatial type or spatial index).
- `TakenAt` is an unvalidated `string`; there is no capture timestamp type.
- There are no indexes on owner, creation date, category or coordinates.

### 2.4 Authentication and ownership

- ASP.NET Core Identity stores users; JWT bearer tokens are issued by `AuthController`.
- JWT is stored in `localStorage` by the Vuex store and sent as a bearer token.
- Upload, update and delete endpoints require authentication.
- Update/delete handlers compare `Photo.CreatedByUserId` with the current user and return 403.
- The public photo feed accepts any `createdByUserId` query value; the “My photos” filter is
  therefore client-directed rather than a server-enforced “mine” query.
- Login does not enable lockout on failure and there is no rate limiting.
- A fixed development JWT signing key is present in `appsettings.json`.

### 2.5 Upload and image processing

- `UploadController` accepts multipart form data, enforces a 10 MB request limit and checks the
  declared `Content-Type` against an allow-list.
- `LocalFileStorage` writes a GUID filename, then tries to generate a 200 px JPEG thumbnail.
- A failed decode is swallowed, leaving the original file on disk and no thumbnail.
- There is no content-signature validation, orientation correction, compression, dimension limit or
  image-processing resource bound.
- Files are stored before the `Photo` row is inserted; a validation or database failure can leave
  orphaned files.
- Non-image content with a permitted MIME type can be stored.

### 2.6 Map and GIS

- `MapRenderer.vue` hardcodes `[40, -74.5]` at zoom 9 (New York).
- After loading photos it calls `fitMapToMarkers`, which overrides the initial view and zooms to all
  photos anywhere in the world.
- Only the OpenStreetMap standard layer is present. There are no layer controls, scale bar,
  fullscreen, locate control, clustering, viewport filtering, results panel or pointer coordinate
  readout.
- Popups show a thumbnail, title, description and details link, but no location name or upload date.
- Upload coordinates can be picked on the map page (`/MapRenderer?pick=1`), but this is a separate
  page flow rather than a picker within the upload form.
- Browser geolocation failure is silent. When permission is denied, the form retains the New York
  default coordinates.
- Coordinate formatting/parsing is duplicated and unvalidated across views.

## 3. Current functionality

### Working

- Registration, login, logout and JWT persistence in `localStorage`.
- SQLite persistence of users, photos, file URLs and thumbnails across restarts.
- Authenticated multipart upload with a 10 MB limit.
- Basic MIME allow-list and thumbnail generation for decodable images.
- Public photo listing and detail retrieval.
- Owner-only update/delete at the API level.
- Leaflet markers with photo popups.
- Photo detail and edit pages.
- Sample Todo CRUD retained from the template.

### Partial or fragile

- “Use my location” updates fields but has no user-facing failure messages.
- Manual coordinates have HTML bounds only, while server validation exceptions are not mapped to a
  400 response in the upload flow.
- File cleanup on delete is best-effort; files can survive a failed database operation.
- Owner-only UI visibility is based on `createdByUserId`; records with a null owner are shown as
  editable to any signed-in user (the API still rejects the change).
- The Vue production build is also run during `dotnet build`, which is convenient but makes backend
  builds depend on Node/npm.

## 4. Confirmed defects

| ID | Severity | Defect | Evidence |
|---|---|---|---|
| D1 | High | Default map is New York, not Port Harcourt | `MapRenderer.vue` line 66; `fitMapToMarkers` line 159 |
| D2 | High | Map always fits all photos on load, defeating the configured default view | `MapRenderer.vue` `loadPhotos` → `fitMapToMarkers` |
| D3 | High | Upload page has no map picker; pick mode is a separate page | `PhotoUpload.vue`; `/MapRenderer?pick=1` |
| D4 | High | Browser geolocation failure is silent and leaves New York defaults in the form | `PhotoUpload.vue` `useCurrentLocation` |
| D5 | High | Upload validation is based on client-declared MIME type only | `UploadController.cs` lines 40–42 |
| D6 | High | Invalid coordinates can fail after the file has been written, returning 500 and leaving an orphan file | `UploadController.cs` saves file before MediatR validation |
| D7 | High | Undecodable files are accepted because thumbnail errors are swallowed | `LocalFileStorage.cs` lines 46–65 |
| D8 | High | No EXIF GPS extraction or DMS/hemisphere handling | No implementation found |
| D9 | High | “My photos” filter trusts a client-supplied user ID | `GetPhotosQuery`; `MapRenderer.vue` |
| D10 | High | Personal email, phone, AbadiGIS branding and placeholder social links are public | `Footer.vue`; `About.vue` |
| D11 | High | Seed data creates a known admin account and sample photos in all environments | `ApplicationDbContextSeed`; `Program.cs` |
| D12 | Medium | No search, category, date, viewport or ownership filtering in the API | `GetPhotosQuery` |
| D13 | Medium | No pagination or gallery view for personal photos | No implementation found |
| D14 | Medium | No marker clustering; popups do not include location or upload date | `MapRenderer.vue` |
| D15 | Medium | `TakenAt` is an unvalidated string and is never populated by upload | `Domain/Entities/Photo.cs`; `UploadController` |
| D16 | Medium | No database indexes for ownership, date, category or coordinate filtering | `PhotoConfiguration.cs` |
| D17 | Medium | SQLite schema cannot be upgraded because startup uses `EnsureCreated` only | `Program.cs` |
| D18 | Medium | No global exception handling or consistent API error response for validation failures | `Startup.cs`; controllers |
| D19 | Medium | Login lockout disabled; no auth rate limiting | `AuthController.cs` line 70 |
| D20 | Medium | Fixed JWT secret in committed configuration | `appsettings.json` |
| D21 | Low | Public API exposes `createdByUserId` in every photo DTO | `PhotoDto` |
| D22 | Low | Broken Vue unit test imports a non-existent `HelloWorld.vue` | `tests/unit/example.spec.js` |
| D23 | Low | README and demo guide document New York sample data and old limitations | `README.md`; `DEMO_GUIDE.md` |
| D24 | Low | No privacy or terms routes although the footer will require them | Router inspection |
| D25 | Low | Home/ map pages use inconsistent American/British spellings and stale copy | Views inspection |

## 5. Missing features

- Configurable Port Harcourt map extent, zoom, limits and location name.
- Layer selection (street, satellite, terrain) with attribution and legal provider use.
- Map controls: reset, fit to photos, fullscreen, locate me, scale bar, pointer coordinates.
- Marker clustering and identical-coordinate handling.
- Upload-page map picker with a draggable marker.
- EXIF GPS detection with DMS conversion and hemisphere handling.
- Optional location name, category, visibility and capture date.
- Server-enforced `mine` filter and owner-aware public queries.
- Search by title/description/location, category filter, date range filter.
- Collapsible photo results panel synchronised bidirectionally with markers.
- Responsive photo gallery with sort, search, category filter and pagination.
- GeoJSON export with correct longitude/latitude order.
- Viewport filtering with efficient bounds predicates.
- Reverse geocoding (optional, failure-tolerant).
- Privacy and terms pages consistent with actual application behaviour.
- Automated coordinate, filtering, ownership and visibility tests.
- Frontend unit tests for coordinate utilities.
- Deployment, verification and enhancement documentation.

## 6. Database assessment

### Current schema

`Photos`:

```
Id                int identity / autoincrement
Title             nvarchar(200) / TEXT, required
Description       nvarchar(2000), nullable
FilePath          nvarchar(500), required
ThumbnailPath     nvarchar(500), nullable
Latitude          float / REAL, required
Longitude         float / REAL, required
TakenAt           nvarchar(max), nullable
CreatedByUserId   nvarchar(max), nullable
CreatedBy         nvarchar(max), required
Created           datetime2 / TEXT, required
LastModifiedBy    nvarchar(max), required
LastModified      datetime2 / TEXT, nullable
```

### Assessment

- The entity correctly separates ownership from authentication and uses an auditable base type.
- `double` coordinates are acceptable for this application’s precision and avoid a provider
  lock-in. The correct approach for SQLite is indexed bounding-box predicates; a spatial type is not
  required for this scale.
- `CreatedByUserId` has no foreign key or index. Ownership is compared string-by-string.
- New optional metadata can be added without rewriting existing rows: nullable columns or
  non-null columns with safe defaults.
- A SQL Server migration and a SQLite compatibility upgrade must both be supplied.
- Existing photo file URLs must remain valid; database changes must not rewrite them.
- Privacy would require file access control. This enhancement uses public/private visibility and
  serves private media through an authorised endpoint, while preserving public static URLs.

## 7. Security findings

| Finding | Risk | Recommendation |
|---|---|---|
| MIME-only upload validation | Malicious or malformed content stored | Decode with ImageSharp, validate pixel limits, re-encode where safe, derive extension from detected format |
| File saved before validation/DB write | Orphan files; 500 responses | Validate coordinates before I/O; delete stored files if DB write fails |
| Client-supplied “mine” user ID | Users can infer other accounts’ uploads | Add server-enforced `mine=true` using the authenticated user |
| Known seeded administrator | Unauthorised production access | Seed only when explicitly enabled in development/demo configuration |
| Hardcoded JWT key | Token forgery if deployed unchanged | Require an environment override outside Development; document key rotation |
| No login lockout/rate limit | Credential stuffing | Enable Identity lockout and a fixed-window rate limiter for auth endpoints |
| Public static upload directory | No access control for private content | Keep public files public; store private files outside web root and serve via authorised endpoint |
| No global error handling | Inconsistent responses and possible information leakage | Add ProblemDetails exception handling and map validation errors to 400 |
| Stack traces | Development only currently, but no production handler | Add production-safe error responses |
| No security headers | Clickjacking, MIME sniffing | Add nosniff, frame-ancestors, referrer policy, permissions policy and HSTS in production |
| Weak password policy | 6 characters, no complexity | Retain existing accounts, raise registration requirements to 8 characters with at least one letter and digit, document |
| No account enumeration protection on registration | Email existence disclosure | Keep generic conflict response text; avoid echoing provider internals |

## 8. UI/UX findings

- The dark navy/blue/light visual identity is coherent and worth preserving.
- The map page spends significant vertical space on a heading card and uses a fixed `min(70vh,
  720px)` canvas. It should fill the available viewport on desktop and remain usable on mobile.
- The upload page is a single narrow card. File selection is a basic browser input; there is no
  preview reset, drag-and-drop, progress, or location context.
- The edit page duplicates coordinate inputs without map assistance and does not expose new
  metadata.
- Photo detail has no location map, copy-coordinate action, external map link, or clear ownership
  state.
- The navbar has no “My photos” page; it links to a query-string map filter.
- The footer has three columns, two of which are personal/organisational contact/branding. It
  should become an application-focused footer.
- Privacy and terms links are missing.
- Accessibility gaps: mobile toggle relies on Bootstrap JavaScript, map canvas has no accessible
  description, notification regions are not consistently live, some controls have small touch
  targets.
- Copy uses a mixture of “PhotoShare/Photo Sharing”, “Log in/Login”, “Share Photo/Share photo” and
  American spellings.

## 9. Recommended implementation plan

1. **Configuration and schema**
   - Add `MapSettings` and `PhotoSettings` configuration.
   - Add `LocationName`, `Category`, `IsPublic` and keep `TakenAt`; add indexes.
   - Add SQL Server migration and idempotent SQLite schema upgrade.
   - Guard seed data behind configuration and default it off outside Development.
2. **API reliability and security**
   - Add server-enforced `mine`, search/category/date/viewport filters, stable sorting and
     pagination.
   - Add global exception/validation handling and security headers.
   - Harden uploads with server-side decoding, pixel limits, orientation, normalisation and orphan
     cleanup.
   - Add EXIF GPS endpoint, settings endpoint, GeoJSON export and authorised media endpoint.
   - Add login lockout and auth rate limiting.
3. **Map**
   - Configure Port Harcourt default; never auto-fit on load.
   - Add clustering, controls, layers, pointer coordinates, filters, results panel and deep links.
4. **Upload and management**
   - Rebuild the upload page around drag-and-drop, preview, map picker, geolocation, EXIF, manual
     entry, metadata and progress.
   - Rebuild edit page with the same coordinate tools.
   - Add My Photos gallery with pagination, sorting, filtering and search.
5. **Branding and documentation**
   - Replace the footer; add privacy and terms pages; remove AbadiGIS personal/organisational data.
   - Update README, audit, enhancement summary, verification report and deployment guide.
6. **Verification**
   - Add coordinate, validation, filtering, visibility, query and API tests; repair the broken Vue
     unit test; add coordinate utility tests.
   - Build the solution, run all tests, exercise the API, and perform browser verification where
     tooling is available.

## 10. Files requiring changes

### Backend

- `src/Domain/Entities/Photo.cs`
- `src/Application/Photos/**`
- `src/Application/Common/Interfaces/IFileStorage.cs`
- New Application interfaces/services for media URLs, EXIF and geocoding
- `src/Infrastructure/Persistence/ApplicationDbContextSeed.cs`
- `src/Infrastructure/Persistence/Configurations/PhotoConfiguration.cs`
- `src/Infrastructure/Persistence/SqliteSchemaUpgrader.cs` (new)
- `src/Infrastructure/Services/LocalFileStorage.cs`
- New Infrastructure services for media URLs, EXIF and geocoding
- `src/Infrastructure/DependencyInjection.cs`
- `src/Infrastructure/Migrations/**` (new migration)
- `src/WebUI/Controllers/PhotosController.cs`
- `src/WebUI/Controllers/UploadController.cs`
- `src/WebUI/Controllers/AuthController.cs`
- New `SettingsController`, geocoding endpoint or equivalent
- `src/WebUI/Startup.cs`
- `src/WebUI/Program.cs`
- `src/WebUI/appsettings*.json`
- `src/WebUI/WebUI.csproj` (packages if required)

### Frontend

- `src/WebUI/clientapp/package.json`
- `src/WebUI/clientapp/src/main.js`
- `src/WebUI/clientapp/src/App.vue`
- `src/WebUI/clientapp/src/router/index.js`
- `src/WebUI/clientapp/src/store/index.js`
- `src/WebUI/clientapp/src/components/NavBar.vue`
- `src/WebUI/clientapp/src/components/Footer.vue`
- `src/WebUI/clientapp/src/views/Home.vue`
- `src/WebUI/clientapp/src/views/MapRenderer.vue`
- `src/WebUI/clientapp/src/views/PhotoUpload.vue`
- `src/WebUI/clientapp/src/views/PhotoEdit.vue`
- `src/WebUI/clientapp/src/views/PhotoDetail.vue`
- New `MyPhotos.vue`, `Privacy.vue`, `Terms.vue`
- New `src/utils/geo.js`, `src/utils/api.js`
- `src/WebUI/clientapp/public/index.html`
- `src/WebUI/clientapp/tests/unit/example.spec.js` (replace with real tests)

### Documentation

- `README.md`
- `DEMO_GUIDE.md`
- `docs/audits/gis-photoshare-existing-system-audit.md`
- `docs/implementation/gis-photoshare-enhancement-summary.md`
- `docs/testing/gis-photoshare-verification-report.md`
- `docs/deployment/gis-photoshare-deployment-guide.md`

## 11. Regression risks

| Risk | Mitigation |
|---|---|
| SQLite database already exists without new columns | Idempotent `ALTER TABLE` upgrade after `EnsureCreated`; test against a legacy-schema database |
| Existing file URLs / records change | Do not rewrite `FilePath`; keep public static URLs; serve legacy paths through the media endpoint fallback |
| Existing JWT tokens invalidated | Keep the same JWT key unless the operator overrides it; no token format change |
| Existing accounts lose access | Do not alter Identity tables or passwords; only add lockout configuration |
| Existing photos become hidden | Visibility defaults to public; map and feed include existing records |
| “Mine” filter breaks old clients | Retain `createdByUserId` support while adding authenticated `mine` |
| Map duplicate instances on navigation | Initialise in `mounted`, destroy in `beforeDestroy`, guard re-entry and call `invalidateSize` on resize |
| New UI breaks mobile | Test at desktop/tablet/mobile widths; collapsible filter panel and drawer |
| New npm dependencies fail the build | Pin versions, install before build, verify `npm ci` |
| Build depends on Node | Existing behaviour; document Node/npm prerequisites and `SkipVueBuild` |

## 12. Audit conclusion

The application is a sound base with working authentication, persistence and ownership checks. The
main work is to make the existing workflows reliable and geographically correct, not to replace the
architecture. Proceeding with an incremental implementation preserves accounts, photo records,
stored files and existing routes while adding the required Port Harcourt GIS experience and
production safeguards.

## 13. Final hardening addendum (23 October 2026)

The findings above describe the pre-enhancement baseline. The implementation now addresses the
D1-D25 findings in the working tree: Port Harcourt configuration, synchronised coordinate tools,
content-decoded image processing, EXIF support, private storage, filters/pagination/GeoJSON,
owner-enforced queries, application-only branding, guarded seeding, SQLite upgrades, consistent
errors/security headers, auth lockout/rate limiting, privacy/terms routes and automated tests are
present. The opaque `CreatedByUserId` is retained in DTOs for backward compatibility; it is not an
email address or credential.

Final hardening additionally fixed duplicate option binding, protected local `.env` files, added a
production JWT-key validator, removed the Windows Event Log startup dependency, made HTTPS/proxy
behaviour explicit, removed vulnerable AutoMapper and patched the .NET dependency graph. The final
NuGet advisory scan reports no vulnerable direct or transitive packages. ImageSharp is patched at
4.1.2 but requires a production licence; Vue 2.7.16 retains one low-severity advisory whose offered
fix is a breaking Vue 3 migration.

Verification passed 69 backend tests, 8 Vue unit tests and 10 Chromium acceptance tests, supplemented
by runtime API and browser checks across the requested workflows. The measured production entrypoint fell from about 518 KiB to 349 KiB.
Local HTTPS, a deployed trusted reverse proxy and physical-device geolocation were not available.

**Final assessment: Ready for controlled staging; not ready for production until the ImageSharp
licence and hosting-topology TLS/proxy checks are completed and the Vue 2 residual risk is accepted
or remediated.** See the verification and deployment documents for evidence and operator steps.
