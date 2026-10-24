# GIS PhotoShare — Demonstration Guide

## 1. What problem does the application solve?

GIS PhotoShare lets users upload photographs with a real-world location and explore them as clustered
markers on an interactive map. It is focused on Port Harcourt, Rivers State, Nigeria, while still
supporting photographs from other places. Owners can manage their own photographs; other users can
explore public contributions.

## 2. Why does the problem matter?

Ordinary photo galleries discard field context. Attaching coordinates to images makes them useful for
community mapping, field observation, environmental monitoring, infrastructure reporting and
location-based storytelling.

## 3. Main capabilities

- Account registration, login, logout and persistent JWT authentication.
- Port Harcourt default map view, with a separate fit-to-photos control.
- Search by title, description or place; category, date and ownership filters.
- Marker clustering, map layers, scale bar, locate-me, fullscreen and pointer coordinates.
- Four ways to set a photograph’s location: device GPS, map picker, manual coordinates, or EXIF GPS.
- Public or private photographs.
- Owner-only edit/delete and a personal gallery.
- GeoJSON export of public photographs.
- Responsive desktop, tablet and mobile layouts with accessible controls.

## 4. Technology summary

| Concern | Technology |
|---|---|
| Backend | ASP.NET Core 8, Clean Architecture, MediatR, FluentValidation |
| Persistence | EF Core 8, SQLite by default; SQL Server optional; InMemory for tests |
| Identity | ASP.NET Core Identity + JWT bearer |
| Images | SixLabors.ImageSharp 4, `exifr` in the browser |
| Front end | Vue 2.6, Vue Router, Vuex, Bootstrap CSS, Leaflet, MarkerCluster |
| Tests | NUnit, FluentAssertions, Moq, Respawn, Playwright, Mocha/Chai |

## 5. Architecture

```
Vue SPA (src/WebUI/clientapp)
  -> /api/auth, /api/photos, /api/upload, /api/settings, /api/geocoding
      -> WebUI controllers and middleware
          -> Application commands/queries, DTOs, validation
              -> Domain entities
              -> Infrastructure (EF Core, Identity, image processing, storage, geocoding)
```

## 6. Database design

| Entity | Purpose | Main fields |
|---|---|---|
| `Photo` | Uploaded image metadata | Id, Title, Description, FilePath, ThumbnailPath, Latitude, Longitude, LocationName, Category, IsPublic, TakenAt, CreatedByUserId, audit fields |
| `ApplicationUser` | Identity account | Id, UserName, Email, password hash |
| `TodoList` / `TodoItem` | Original template demo, not exposed in the UI | Template fields |

SQLite creates a schema with `EnsureCreated` and runs an idempotent upgrader for existing databases.
SQL Server uses migrations. The new metadata columns default existing photographs to public and the
`General` category.

## 7. Starting the demonstration

```powershell
cd src\WebUI\clientapp
npm ci
cd ..\..\..
dotnet build gis-photo-sharing-app.sln
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\WebUI --launch-profile WebUI
```

Open <http://localhost:5000/>.

### Development account

| Email | Password |
|---|---|
| `administrator@localhost` | `Administrator1!` |

Development seeding is enabled in `appsettings.Development.json`. It creates the account and three
demonstration photographs around Port Harcourt only when the database is empty.

## 8. Demonstration sequence

1. **Home page.** Show the application purpose, Port Harcourt focus and “Explore the map” action.
2. **Open the map.** Point out that it opens at Port Harcourt zoom 12 without fitting distant photos.
   Show the results panel, cluster markers, layer control and scale bar.
3. **Map tools.** Use “Fit photos”, then “Port Harcourt”; click a result to centre and open its popup;
   move the pointer to show live coordinates; copy them.
4. **Search and filters.** Search for a title or place, select a category and a date range, enable
   “My photos only”. Clear the filters. Use “Search this area” to restrict to the current viewport.
5. **Create an account or log in.** Sign in with the development account.
6. **Share a photograph.**
   - Drag an image into the drop zone or browse.
   - Observe the preview and any detected EXIF GPS suggestion.
   - Click “Use detected location” (if available) or pick a point on the map; drag the pin.
   - Enter title, description, location name, category, capture date and visibility.
   - Submit and observe the progress and success panel.
7. **View on map.** Follow the deep link; the map centres on the uploaded photograph and opens its
   popup. Show the new marker.
8. **My photos.** Open the gallery, search/sort/filter, preview the card and edit the title, location or
   coordinates. Save and observe the detail page update.
9. **Private visibility.** Edit a photograph to “Only me”, then show that it is absent from the public
   map feed and GeoJSON but still visible to its owner.
10. **GeoJSON.** Export the current public dataset and point out longitude-first coordinates.
11. **Owner security.** Register a second user. Attempt to edit or delete the first user’s photo through
    the API; show `403 Forbidden`. The UI hides the actions and the server enforces ownership.
12. **Validation.** Try an invalid latitude, a malformed image renamed with `.jpg`, or an oversized
    image. Show the user-facing error.
13. **Footer and policies.** Show the application-focused footer (no personal phone/email), dynamic
    copyright and working privacy/terms links.
14. **Mobile.** Use a narrow viewport: open the navigation menu, show/hide the map results drawer and
    verify the upload form remains usable.

## 9. Talking points for assessment

- Why the default map is a configuration value, not hardcoded in multiple components.
- Difference between stored EPSG:4326 coordinates, Leaflet’s latitude-first convention and GeoJSON’s
  longitude-first convention.
- Why the “My photos” filter must be enforced on the server.
- How DMS EXIF coordinates are converted to decimal degrees with hemisphere handling.
- How the server verifies image content instead of trusting MIME types.
- How private media is protected without exposing a static file path.
- How the SQLite compatibility upgrader preserves existing records.
- Why `fitBounds` is not called on initial load when the product requires a fixed default view.

## 10. Automated evidence

```powershell
dotnet test gis-photo-sharing-app.sln
cd src\WebUI\clientapp
npm run test:unit
npm run test:e2e
```

At handover: 69 backend tests, 8 Vue unit tests and 10 Playwright tests passed. See
`docs/testing/gis-photoshare-verification-report.md` for the full results and limitations.

## 11. Limitations and future work

- SQLite/local disk is suitable for a single-instance demonstration, not distributed production.
- Email confirmation, password reset and account self-deletion are not implemented.
- Reverse geocoding depends on Nominatim and is optional.
- External map tiles require network access.
- Formal accessibility audit, Firefox/Safari verification, load testing and production deployment
  validation remain outstanding.
- Possible future work: albums, comments, moderation, spatial database indexes, offline map support and
  a fully managed media storage adapter.
