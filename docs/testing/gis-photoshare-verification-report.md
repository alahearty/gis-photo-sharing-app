# GIS PhotoShare — Verification Report

**Verification date:** 23 October 2026  
**Environment:** Windows, PowerShell 5.1, .NET SDK 10.0.402 building `net8.0`, Node.js 22.19.0,
npm 10.9.3, Chrome/Chromium available for Playwright, SQL Server LocalDB 15 installed.

This report distinguishes **verified** behaviour from items that were implemented but not exercised
in this environment. A backend unit-test pass is not treated as evidence that a browser workflow
works; browser workflows were exercised separately.

## 1. Build verification

Command:

```powershell
dotnet build gis-photo-sharing-app.sln
```

Result: **Build succeeded, 0 errors**. The build also ran the Vue production build from
`src/WebUI/clientapp`.

NuGet vulnerability audit:

```powershell
dotnet list gis-photo-sharing-app.sln package --vulnerable --include-transitive
```

Result: **no vulnerable packages** reported for any project. AutoMapper was removed and EF Core /
ImageSharp were upgraded during the enhancement.

Frontend production audit:

```powershell
cd src\WebUI\clientapp
npm audit --omit=dev
```

Result: one **low** severity Vue 2 ReDoS advisory. The available automatic fix requires Vue 3, which
would be a major frontend replacement; it was not applied. This is recorded as a known limitation.

## 2. Automated test results

Command:

```powershell
dotnet test gis-photo-sharing-app.sln --no-build
```

| Suite | Result |
|---|---|
| `Domain.UnitTests` | **5 passed** |
| `Application.UnitTests` | **34 passed** |
| `Applicaton.IntegrationTests` | **30 passed** |
| **Backend total** | **69 passed, 0 failed** |

Command:

```powershell
cd src\WebUI\clientapp
npm run test:unit
```

Result: **8 passing, 0 failing** coordinate utility tests.

### Covered backend behaviour

- Coordinate validation boundaries and invariant parsing expectations.
- Leaflet `[lat, lng]` and GeoJSON `[lng, lat]` ordering in client utilities.
- Public/private query visibility and authenticated `mine` filtering.
- Search, category, date and bounding-box filtering, including case-insensitive search/category
  translation on real SQLite.
- Gallery pagination and total counts.
- Owner-only update/delete; non-owner rejection.
- GeoJSON excludes private photos and uses longitude-first coordinates.
- EXIF DMS conversion for north/east and south/west, capture date, no-EXIF and malformed input.
- Server-side image decoding, public/private storage paths, thumbnail creation, metadata stripping,
  malformed-content rejection and private-file deletion.
- Media URL token issuance and binding to photo id/resource type.
- SQLite legacy schema upgrade with row preservation and idempotent second run.
- Configuration hardening for nested environment variables, photo category normalisation, safe
  defaults and production JWT-key validation.
- SQL Server integration suite creates/resets `gis_photo_sharing_appTestDb` and passes the new
  migration.

## 3. Playwright end-to-end suite

Command:

```powershell
cd src\WebUI\clientapp
npm run test:e2e
```

Result: **10 passed, 0 failed** in approximately 44 seconds.

The suite starts the API in Development with an in-memory database, seeds the demo account and three
Port Harcourt samples, and stores uploads under `artifacts/e2e-storage` rather than the development
database/media.

| Test | Result |
|---|---|
| Homepage, navigation and application-focused footer render | Passed |
| Map starts at Port Harcourt and reset restores the configured view | Passed |
| Manual coordinates, map picking, marker dragging and validation stay synchronised | Passed |
| Geolocation granted and denied states are handled | Passed |
| Valid private upload succeeds, remains private and can be removed | Passed |
| Malformed image upload is rejected | Passed |
| Marker popup, search, category and my-photos filters work | Passed |
| Owner can edit/delete while another user is forbidden | Passed |
| Mobile and tablet layouts keep navigation and map usable | Passed |
| API failure and empty-state paths are explicit; key pages avoid console errors | Passed |

## 4. Additional runtime API verification

A separate runtime script exercised a temporary SQLite database through the built application and
reported **all checks passed**:

- Health endpoint and SPA index.
- `GET /api/settings` returned Port Harcourt defaults (`4.8156`, `7.0498`, zoom 12) and configurable
  categories.
- Development seed contained Port Harcourt photographs at the configured coordinates.
- `GET /api/photos?mine=true` returned `401` without a token.
- Existing demo user login returned a JWT.
- Valid image upload returned `201`; latitude and longitude persisted in the correct order; metadata
  persisted; public original and thumbnail returned `200`.
- Valid latitude precision was retained by the API while the UI displays six decimals.
- Invalid coordinates returned `400`.
- A non-image file declared as `image/jpeg` returned `400`.
- `POST /api/upload/exif` accepted a real EXIF GPS JPEG and returned the expected coordinates.
- Private upload was excluded from the anonymous feed and detail; the owner could read it; a signed
  private media URL returned `200`; removing the token returned `404`.
- `mine=true` included the owner’s private photo; the paginated gallery returned a total count and
  items.
- Search and category filtering returned/excluded the expected records.
- Owner update returned `204` and changed title, coordinates, location, category and capture date.
- A second user received `403` on update and delete.
- GeoJSON was a valid feature collection, used `[longitude, latitude]` and excluded private photos.
- SPA history fallback served `/Map`.
- Unknown `/api` route returned a JSON `404`.
- Owner delete returned `204`; the deleted photo then returned `404`.

## 5. Additional browser verification

A separate Playwright-core/Chrome smoke run against a temporary SQLite database verified
**15 checks**, including:

- Server EXIF extraction with a generated GPS JPEG.
- Footer contains no personal phone, personal email, `@` address, AbadiGIS branding or old social
  links.
- Initial map centre/zoom remains Port Harcourt even with a distant New York photograph present; the
  camera is not auto-fitted.
- Search results and clearing filters update the panel.
- Fit-to-photos lowers the zoom and return-to-Port Harcourt restores the configured view.
- Browser login persists and shows the profile menu.
- EXIF suggestion card appears; applying it fills `4.809167` and `7.049722`.
- Geolocation denial is explained instead of silently substituting coordinates.
- Upload succeeds through the browser with success state and deep link.
- The uploaded photo appears at its saved coordinates and its popup opens.
- Marker and results selection synchronise.
- My Photos edit updates metadata and location.
- Delete confirmation removes the photo from the gallery.
- Mobile results drawer and navigation menu work.
- No browser page errors occurred.

## 6. Existing data compatibility verification

The workspace’s existing SQLite database was opened by the enhanced application without deleting or
rewriting records. The API returned four existing public photographs and both original media files
returned HTTP `200`:

- One older persistence-test photograph outside Port Harcourt.
- Three older demo photographs from the original New York seed.

This confirms that existing records and file references remain accessible. The Port Harcourt map does
not auto-fit these distant records on load.

The SQLite legacy-schema automated test independently creates an old-format `Photos` table, inserts a
row, runs the upgrader twice, and confirms:

- the row survives;
- title, file path, coordinates and owner are unchanged;
- `LocationName` is null;
- `Category` becomes `General`;
- `IsPublic` defaults to true;
- the second upgrade is a no-op.

## 7. Not verified in this environment

The following were implemented or documented but not proven by the automated runs above:

- A real production deployment behind HTTPS/IIS/nginx, including certificate and proxy configuration.
- Production JWT startup failure on a real production host (the validator itself is unit-tested).
- Live reverse geocoding against Nominatim over the public internet (the UI handles failures; no live
  provider call was asserted).
- Physical GPS accuracy on a mobile device (browser geolocation was simulated/denied in Chromium).
- Fullscreen API, map layer switching, scale bar and the map locate-me control were not individually
  asserted in the browser suite.
- GeoJSON file download through the browser button (the GeoJSON endpoint itself was validated).
- Viewport (“Search this area”) filtering and date filters through the browser (date filtering is
  covered by backend tests).
- PNG, GIF and WebP uploads end-to-end (JPEG and malformed content were exercised; format handling is
  implemented and limited by ImageSharp).
- Account lockout and rate-limiter behaviour under sustained attack.
- Rollback of the SQL Server migration.
- Firefox and Safari compatibility.
- Load, stress, multi-instance and large-collection performance.
- WCAG conformance with an automated accessibility scanner. Basic keyboard focus, labels and ARIA
  attributes were reviewed manually; formal audit remains outstanding.
- Email confirmation, password reset and account deletion (not implemented).
- Database backup/restore procedures in a live production system.

## 8. Commands for reproduction

```powershell
# Restore, build and test
dotnet build gis-photo-sharing-app.sln
dotnet test gis-photo-sharing-app.sln

# Frontend unit and browser tests
cd src\WebUI\clientapp
npm ci
npm run test:unit
npm run test:e2e

# Run the application
cd ..\..\..
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\WebUI --launch-profile WebUI
```

## 9. Conclusion

The enhancement builds successfully, preserves existing data, and passes backend, frontend unit,
browser end-to-end and additional API/browser verification. The verified results support the reported
features. The items in section 7 remain limitations or require a production environment to validate.
