# Database Notes

## Default: SQLite

The default configuration in `src/WebUI/appsettings.json` and
`src/WebUI/appsettings.Development.json` uses:

```json
"DatabaseProvider": "Sqlite",
"UseInMemoryDatabase": false,
"ConnectionStrings": {
  "SqliteConnection": "Data Source=gis_photo_sharing_app.db"
}
```

On first start the API creates:

```
src/WebUI/App_Data/gis_photo_sharing_app.db
```

A relative SQLite path is resolved under the application content root (`src/WebUI`), and the
`App_Data` directory is created automatically. Metadata persists across restarts.

### Existing databases

SQLite mode uses `EnsureCreated` for a new database and then runs
`SqliteSchemaUpgrader`. The upgrader checks `PRAGMA table_info('Photos')` and applies only missing
changes:

- `LocationName` nullable text
- `Category` text with default `General`
- `IsPublic` integer with default `1`
- Indexes for owner, creation date, category, visibility and coordinates

Existing rows are preserved. In particular, no coordinates are invented or rewritten, and existing
photo ownership and file paths remain unchanged. The operation is idempotent and was tested against a
legacy `Photos` table containing a pre-existing row.

The upgrader does not replace EF migrations and does not rebuild tables. Back up the SQLite file
before a major upgrade. To roll back a column addition in SQLite, restore the backup; older SQLite
versions cannot reliably drop columns in place.

### Development seeding

Seeding is opt-in:

```json
"DatabaseSeed": {
  "DefaultUser": true,
  "SamplePhotos": true
}
```

`appsettings.Development.json` enables both values. `appsettings.json` disables both, so a production
run does not create a known administrator or sample photographs. The sample data is inserted only when
the photo table is empty.

To reset a development database, stop the application and move or delete `src/WebUI/App_Data`. Do
not delete `wwwroot/uploads` unless you also want to remove public media.

### File locations

| Content | Default location |
|---|---|
| Public images | `src/WebUI/wwwroot/uploads/photos` |
| Public thumbnails | `src/WebUI/wwwroot/uploads/thumbnails` |
| Private images | `src/WebUI/App_Data/private-uploads/photos` |
| Private thumbnails | `src/WebUI/App_Data/private-uploads/thumbnails` |

`PhotoSettings:StorageRoot` can relocate both public and private storage under one root. Public files
are then written to `{StorageRoot}/public`, private files to `{StorageRoot}/private`. Legacy public
files under `wwwroot/uploads` remain readable.

## Optional: SQL Server

Set:

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=gis_photo_sharing_app;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Apply migrations:

```powershell
dotnet ef database update --project src\Infrastructure --startup-project src\WebUI
```

### Current photo schema

The `Photos` table stores:

| Column | Notes |
|---|---|
| `Id` | Primary key |
| `Title` | Required, maximum 200 characters |
| `Description` | Optional, maximum 2,000 characters |
| `FilePath` | Required storage reference (public URL-like path or `private://` reference) |
| `ThumbnailPath` | Optional storage reference |
| `Latitude`, `Longitude` | WGS 84 doubles with six-decimal UI precision |
| `LocationName` | Optional place label, maximum 200 characters |
| `Category` | Optional configured category, maximum 50 characters |
| `IsPublic` | Public/private visibility; existing rows default to public |
| `TakenAt` | Optional capture date/time string |
| `CreatedByUserId` | Owner id (indexed) |
| `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy` | Audit columns (creation date indexed) |

### Migration and rollback

The SQL Server migration `AddPhotoMetadataAndIndexes`:

- adds the three metadata/visibility columns;
- sets `Category = 'General'` for existing rows;
- creates indexes on owner, creation date, category, visibility and coordinates;
- does **not** alter existing coordinates, file paths, titles or ownership.

Rollback (`Down`) drops the new indexes and columns. Dropping the columns destroys visibility,
location-name and category values for all photographs, so take a database backup before downgrading.
It does not delete image files. Prefer restoring a backup over running `Down` in production.

## Tests

The integration test project uses `gis_photo_sharing_appTestDb` in SQL Server LocalDB and a separate
temporary SQLite file. The SQLite tests cover:

- metadata persistence across context recreation;
- idempotent legacy-schema upgrade with row preservation.

The Playwright suite uses an isolated in-memory database and `artifacts/e2e-storage`, so it does not
touch the development database or media.
