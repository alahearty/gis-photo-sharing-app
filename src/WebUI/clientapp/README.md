# GIS PhotoShare client

Vue 2 single-page client for GIS PhotoShare. The API normally serves the built client from
`clientapp/dist`; the standalone development server proxies `/api` and `/uploads` to the backend.

## Setup

```powershell
npm ci
```

## Development server

```powershell
npm run serve
```

Vue CLI serves <http://localhost:8080> and proxies to `https://localhost:44387` (IIS Express) by
default. For Kestrel on ports 5000/5001:

```powershell
$env:VUE_APP_DEV_API_TARGET = 'https://localhost:5001'
npm run serve
```

Set `VUE_APP_API_URL` in `clientapp/.env.local` only when the API is on a separate origin. Leave it
empty when the API serves the SPA.

## Build

```powershell
npm run build
```

The WebUI project normally invokes this automatically. The npm scripts set
`NODE_OPTIONS=--openssl-legacy-provider` for compatibility with the Vue CLI 4/webpack toolchain on
modern Node versions.

## Tests

```powershell
npm run test:unit   # Mocha/Chai coordinate utility tests
npm run test:e2e    # Playwright release-readiness suite
```

The Playwright configuration starts the API on <http://127.0.0.1:5397> with an in-memory database and
isolated media under `artifacts/e2e-storage`. Install a browser first if Playwright reports one is
missing:

```powershell
npx playwright install chromium
```

## Lint

```powershell
npm run lint
```

## Key structure

```
src/components       Navigation, footer, location picker, confirmation dialog
src/views            Home, map, upload, gallery, detail, edit, auth and policy pages
src/utils            API client, coordinate helpers, settings loader
tests/unit           Coordinate utility tests
tests/e2e            Playwright release-readiness tests
```

Coordinates are stored and transported as WGS 84 (EPSG:4326) latitude/longitude. Leaflet uses
`[latitude, longitude]`; GeoJSON uses `[longitude, latitude]`. The shared helpers in
`src/utils/geo.js` are the canonical client-side conversions.
