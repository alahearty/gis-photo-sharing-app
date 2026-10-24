# GIS PhotoShare — Enhancement Summary

**Implementation date:** 23 October 2026  
**Approach:** Incremental enhancement of the existing ASP.NET Core 8 / Vue 2 application. Existing
accounts, photo records, ownership relationships and stored media were preserved.

## 1. Scope delivered

The enhancement implements the Port Harcourt GIS focus, end-to-end coordinate handling, a modern map
and upload experience, owner-controlled gallery workflows, security hardening, database compatibility
and automated verification.

## 2. Architecture retained

The existing Clean Architecture layering, MediatR commands/queries, FluentValidation pipeline, EF
Core persistence, Identity/JWT authentication, Vue 2 SPA and Leaflet map were retained. No
microservices, external paid services or database migration were introduced.

Changes were made within the existing layers:

| Layer | Main additions |
|---|---|
| Domain | Photo metadata/visibility fields |
| Application | Filtering, paging, GeoJSON, file access, EXIF and media URL abstractions; validation |
| Infrastructure | Schema upgrade, hardened image storage, EXIF reader, media tokens, geocoding |
| WebUI | New controllers/middleware, security configuration, rebuilt Vue pages and tests |

## 3. GIS and map enhancements

### Port Harcourt configuration

- Added configurable `MapSettings` with the Port Harcourt default:
  - latitude `4.8156`
  - longitude `7.0498`
  - zoom `12`
  - minimum zoom `3`, maximum `19`
  - location name `Port Harcourt, Rivers State, Nigeria`
- Exposed settings through `GET /api/settings`; the client also has safe fallbacks.
- Removed the previous New York hardcode and the automatic fit-to-all-photos on load. Distant photos
  no longer change the initial view.
- Added “Return to Port Harcourt” and “Fit photos” controls.

### Map tools

- Leaflet zoom, scale bar, layer control, fullscreen, locate-me, pointer coordinate readout, copy
  coordinates and click-to-pick mode.
- Marker clustering with count bubbles, spiderfying and identical-coordinate handling.
- Photo markers with popups showing thumbnail, title, description, location, category, upload date
  and a details link.
- Search by title/description/location, category and date filters, server-enforced “My photos only”
  and viewport (“Search this area”) filtering.
- Bidirectional synchronisation between the map and a collapsible results panel.
- Empty, loading, error and tile-failure states.
- Responsive layouts: sidebar on desktop; map with collapsible results and filters on mobile.
- `invalidateSize()` is used after layout changes and container resizing; the map is created once and
  destroyed on navigation.

### Layers and attribution

- OpenStreetMap standard (default).
- OpenTopoMap terrain (maximum zoom 17).
- Esri World Imagery satellite tiles.
- Visible attribution and Leaflet attribution control. No undocumented or key-requiring endpoints
  were introduced.

### Coordinate conventions

- Stored coordinates: WGS 84 / EPSG:4326 latitude and longitude doubles.
- Leaflet positions: `[latitude, longitude]`.
- GeoJSON positions: `[longitude, latitude]`.
- Client display: six decimal places (`4.815600, 7.049800`).
- Server parsing is culture-invariant and validates ranges (`-90..90`, `-180..180`) and finiteness.
- Shared `src/utils/geo.js` utilities are covered by unit tests.
- Leaflet marker coordinates are updated without feedback loops by guarding map-originated updates.

## 4. Coordinate acquisition

The upload page and edit page support:

1. **Use my location** — browser geolocation with accuracy, permission denial, unavailable service,
   timeout and HTTPS/localhost explanations. Port Harcourt is never silently substituted.
2. **Pick on map** — embedded draggable marker; click or drag updates the coordinate fields.
3. **Manual entry** — trimmed text fields with client and server range validation and normalisation to
   six decimal places on blur.
4. **Image GPS metadata** — client-side `exifr` detection with a suggestion card; the user explicitly
   applies detected coordinates. The server also exposes `POST /api/upload/exif` using ImageSharp and
   supports DMS-to-decimal conversion with hemisphere handling.

The reverse-geocoding “Find place” action is optional, rate-limited, attributed and failure-tolerant.

## 5. Upload experience

- Drag-and-drop and click-to-browse file selection.
- Image preview, file type and size validation, clear/reset, upload progress, duplicate-submit guard
  and success state with “View on map”, “My photos”, “View details” and “Upload another” actions.
- Metadata: title, description, location name, category, capture date, visibility and upload date.
- Categories are configured through `PhotoSettings:Categories` and served to the client.
- Public/private visibility selects the appropriate storage and API access rules.
- Client validation is duplicated by authoritative server validation; files are not saved until the
  basic form values are valid.

### Server-side image processing

- Request limit approximately 10.5 MB with a 10 MB file limit.
- Decoding with ImageSharp validates actual content rather than trusting the declared MIME type.
- Allowed formats: JPEG, PNG, GIF and WebP.
- Pixel and dimension limits; single-frame decoding prevents animated decompression abuse.
- EXIF auto-orientation and maximum-width resizing.
- EXIF/XMP/IPTC metadata is removed from stored media after orientation, preventing hidden GPS or
  camera metadata from bypassing the visibility/location choices made in the form.
- Re-encoding with sensible quality; JPEG thumbnails at 360 px.
- Collision-free GUID names and safe path handling.
- Private files stored outside the web root.
- Database failure triggers cleanup of the newly stored file.
- Existing legacy files and URLs continue to work.

## 6. Gallery and ownership

- New `/MyPhotos` gallery with responsive grid, lazy-loaded thumbnails, title/location/category/date,
  public/private badge, search, category filter, sort (newest, oldest, title ascending/descending),
  pagination and empty/error states.
- Edit page supports metadata, coordinates, map picker, place lookup, visibility and capture date.
- Detail page supports large image preview, metadata, map location, coordinate copy, OpenStreetMap
  link, map deep link and owner edit/delete.
- Delete uses an accessible confirmation dialog.
- Ownership is enforced in backend handlers and media access, not only by hiding UI controls.
- Existing ownership data is unchanged.

## 7. Footer, navigation and policy pages

- Removed personal email, telephone number, personal names, physical contact details, AbadiGIS
  branding and placeholder social links from the public footer and About page.
- New application-focused footer with Home, Explore map, Share photo, About, Privacy and Terms links,
  dynamic copyright year and map attribution.
- Added Privacy and Terms pages describing actual application behaviour, clearly marked for owner and
  legal review. No contact information or registration details were invented.
- Added profile menu, mobile navigation, active link states and persistent auth state across reloads.
- Added a 404 page and a skip-to-content link.

## 8. Authentication and security

- Retained ASP.NET Core Identity and JWT authentication.
- Added server-enforced `mine=true` using the authenticated user rather than a client-supplied id.
- Added fixed-window rate limiting for authentication endpoints and Identity lockout.
- Raised registration password requirements to 8 characters with a letter and number. Existing
  account passwords are unaffected.
- Registration failure messages no longer expose provider internals.
- Added production JWT-key validation (length, entropy and placeholder rejection).
- Added ProblemDetails exception handling for validation, invalid images, oversized requests and
  unexpected failures; no stack traces are returned.
- Added security headers (`nosniff`, frame denial, referrer policy, permissions policy, COOP).
- Production HSTS and configurable HTTPS redirection.
- Added private-media access tokens bound to the photo id, resource type and a 30-minute expiry.
- Restricted the legacy direct-create endpoint to safe public `/uploads/` paths and public records.
- CORS remains restricted to development origins.
- No default production administrator credentials. Seeding is disabled outside Development by default.
- NuGet audit reports no vulnerable direct or transitive packages after upgrading EF Core and
  removing AutoMapper.

## 9. API and reliability

New/updated endpoints:

```
GET    /api/settings
GET    /api/photos?mine=&search=&category=&from=&to=&minLat=&maxLat=&minLng=&maxLng=&sort=
GET    /api/photos/gallery?mine=&search=&category=&sort=&page=&pageSize=
GET    /api/photos/geojson
GET    /api/photos/{id}
GET    /api/photos/{id}/file
GET    /api/photos/{id}/thumbnail
POST   /api/upload/photo
POST   /api/upload/exif
POST   /api/photos            (legacy public-path creation, restricted)
PUT    /api/photos/{id}
DELETE /api/photos/{id}
GET    /api/geocoding/reverse
GET    /health
```

- `PhotoDto` adds image/thumbnail URLs, location name, category, visibility, creation/update dates and
  owner state while retaining legacy `FilePath`/`ThumbnailPath` fields.
- Public feeds include public photos; authenticated owners also see their private photos.
- GeoJSON exports public photos only and uses correct longitude/latitude ordering.
- Paging is capped at 96 items per page.
- Unknown `/api` routes return JSON 404 instead of the SPA.
- Stable ordering and indexed query columns are used.
- API errors use consistent statuses (`400`, `401`, `403`, `404`, `413`, `429`, `500`).

## 10. Database changes

Added to `Photo`:

- `LocationName` (optional, 200)
- `Category` (optional, 50; existing rows assigned `General`)
- `IsPublic` (required, default true)

Added indexes:

- `CreatedByUserId`
- `Created`
- `Category`
- `IsPublic`
- `(Latitude, Longitude)`

SQL Server migration: `20261023010646_AddPhotoMetadataAndIndexes`.  
SQLite compatibility: `SqliteSchemaUpgrader` performs idempotent `ALTER TABLE ADD COLUMN` and
`CREATE INDEX IF NOT EXISTS` operations. Existing rows and file references are not rewritten.

## 11. UI/UX and accessibility

- Retained the dark navy, blue and light visual identity.
- Map uses viewport-aware height; upload uses a two-column form/preview/picker layout; mobile layouts
  stack cleanly.
- Added visible focus indicators, semantic headings, labelled controls, live regions, keyboard map
  controls, touch-sized actions, lazy image loading and reduced-motion support.
- British English used for new user-facing labels and messages.
- Upload page heading is not obscured by the navigation bar.
- Map results and markers stay synchronised in both directions.

## 12. Documentation and tests added

- Existing-system audit.
- This enhancement summary.
- Verification report.
- Deployment guide.
- Updated README, demo guide and database notes.
- Backend tests for coordinate validation, query filtering/visibility, gallery paging, GeoJSON,
  owner commands, EXIF DMS conversion, media tokens, SQLite upgrade and configuration hardening.
- Vue coordinate utility tests.
- Playwright release-readiness suite covering authentication, map defaults, coordinates, geolocation,
  private uploads, malformed files, search/filters, owner edit/delete, mobile layout, API error and
  empty states.

## 13. Backward compatibility

- Existing user accounts, password hashes, photo rows, ownership and stored public file paths remain
  valid.
- Existing SQLite databases are upgraded in place.
- Existing `/uploads/...` media remains readable; the media endpoint falls back to legacy storage.
- Existing routes (`/MapRenderer`, `/PhotoUpload`, `/photos/{id}`, `/photos/{id}/edit`) remain.
  `/Map` and `/login` style aliases were added without removing originals.
- Legacy `createdByUserId` photo filtering remains supported; the new authenticated `mine` filter is
  preferred.
- Existing API clients can continue to use public `FilePath`/`ThumbnailPath`; new clients should use
  `imageUrl`/`thumbnailUrl` so private-media tokens are handled correctly.
- The demo Todo endpoints remain from the original template but are not exposed in the UI.
