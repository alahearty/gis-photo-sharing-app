const path = require("path");
const fs = require("fs");
const { test, expect } = require("@playwright/test");

const admin = { email: "administrator@localhost", password: "Administrator1!" };
const sampleImage = path.resolve(__dirname, "../../../wwwroot/uploads/sample1.jpg");

async function loginApi(request, credentials = admin) {
  const response = await request.post("/api/auth/login", { data: credentials });
  expect(response.ok()).toBeTruthy();
  return response.json();
}

async function authenticatePage(page, request, credentials = admin) {
  const auth = await loginApi(request, credentials);
  await page.addInitScript((value) => {
    localStorage.setItem("gis_photo_auth", JSON.stringify({
      token: value.token,
      user: { id: value.userId, email: value.email },
    }));
  }, auth);
  return auth;
}

async function createPhoto(request, auth, overrides = {}) {
  const response = await request.post("/api/upload/photo", {
    headers: { Authorization: `Bearer ${auth.token}` },
    multipart: {
      title: overrides.title || `E2E photo ${Date.now()}`,
      description: "Created by the isolated Playwright suite",
      latitude: String(overrides.latitude || 4.8156),
      longitude: String(overrides.longitude || 7.0498),
      locationName: "Port Harcourt",
      category: overrides.category || "General",
      isPublic: String(overrides.isPublic ?? true),
      takenAt: "2026-10-23",
      file: { name: "sample1.jpg", mimeType: "image/jpeg", buffer: fs.readFileSync(sampleImage) },
    },
  });
  expect(response.status()).toBe(201);
  return (await response.json()).id;
}

async function deletePhoto(request, auth, id) {
  const response = await request.delete(`/api/photos/${id}`, {
    headers: { Authorization: `Bearer ${auth.token}` },
  });
  expect([204, 404]).toContain(response.status());
}

test("homepage, navigation and application-focused footer render", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: /share photographs on the map/i })).toBeVisible();
  await page.getByRole("link", { name: /explore map/i }).first().click();
  await expect(page).toHaveURL(/MapRenderer/i);
  await expect(page.getByRole("application", { name: /interactive map/i })).toBeVisible();
  await page.goto("/");
  const footer = page.locator("footer");
  await expect(footer).toContainText("GIS PhotoShare");
  await expect(footer).toContainText("All rights reserved");
  await expect(footer).not.toContainText(/@|telephone|phone|AbadiGIS/i);
});

test("map starts at Port Harcourt and reset restores the configured view", async ({ page }) => {
  await page.goto("/MapRenderer");
  await expect(page.locator(".leaflet-container")).toBeVisible();
  const initial = await page.locator(".map-canvas").evaluate(() => {
    const component = Array.from(document.querySelectorAll("*")).map((element) => element.__vue__).find((vue) => vue && vue._map);
    const map = component._map;
    const centre = map.getCenter();
    const zoom = map.getZoom();
    map.setView([10, 10], 5);
    return { lat: centre.lat, lng: centre.lng, zoom };
  });
  expect(initial.lat).toBeCloseTo(4.8156, 3);
  expect(initial.lng).toBeCloseTo(7.0498, 3);
  expect(initial.zoom).toBe(12);
  await page.getByRole("button", { name: "Port Harcourt", exact: true }).click();
  await expect.poll(async () => page.locator(".map-canvas").evaluate(() => {
    const component = Array.from(document.querySelectorAll("*")).map((element) => element.__vue__).find((vue) => vue && vue._map);
    const map = component._map;
    const centre = map.getCenter();
    return Math.abs(centre.lat - 4.8156) < 0.0005
      && Math.abs(centre.lng - 7.0498) < 0.0005
      && map.getZoom() === 12;
  })).toBe(true);
  const reset = await page.locator(".map-canvas").evaluate(() => {
    const component = Array.from(document.querySelectorAll("*")).map((element) => element.__vue__).find((vue) => vue && vue._map);
    const map = component._map;
    return { lat: map.getCenter().lat, lng: map.getCenter().lng, zoom: map.getZoom() };
  });
  expect(reset.lat).toBeCloseTo(4.8156, 3);
  expect(reset.lng).toBeCloseTo(7.0498, 3);
  expect(reset.zoom).toBe(12);
});

test("manual coordinates, map picking, marker dragging and validation stay synchronised", async ({ page, request }) => {
  await authenticatePage(page, request);
  await page.goto("/PhotoUpload");
  await page.locator('input[type="file"]').setInputFiles(sampleImage);
  await page.locator("#photo-title").fill("Coordinate validation test");
  const latitude = page.locator("#photo-latitude");
  const longitude = page.locator("#photo-longitude");
  await latitude.fill("4.900000");
  await longitude.fill("7.100000");
  await longitude.blur();
  await expect(page.locator(".location-picker .leaflet-marker-icon")).toBeVisible();
  const map = page.locator(".location-picker__map");
  const box = await map.boundingBox();
  await map.click({ position: { x: box.width * 0.25, y: box.height * 0.25 } });
  await expect(latitude).not.toHaveValue("4.900000");
  const markerBox = await page.locator(".location-picker .leaflet-marker-icon").boundingBox();
  await page.mouse.move(markerBox.x + markerBox.width / 2, markerBox.y + markerBox.height / 2);
  await page.mouse.down();
  await page.mouse.move(markerBox.x + 35, markerBox.y + 20, { steps: 5 });
  await page.mouse.up();
  expect(Number(await latitude.inputValue())).toBeGreaterThanOrEqual(-90);
  await latitude.fill("91");
  await page.getByRole("button", { name: "Share photo" }).click();
  await expect(page.getByText(/Enter a valid latitude \(-90 to 90\)/i)).toBeVisible();
});

test("geolocation granted and denied states are handled", async ({ page, context, request }) => {
  await authenticatePage(page, request);
  await context.grantPermissions(["geolocation"], { origin: "http://127.0.0.1:5397" });
  await context.setGeolocation({ latitude: 4.823456, longitude: 7.012345, accuracy: 12 });
  await page.goto("/PhotoUpload");
  await page.getByRole("button", { name: /use my location/i }).click();
  await expect(page.locator("#photo-latitude")).toHaveValue("4.823456");
  await expect(page.getByText(/Location found, accurate/i)).toBeVisible();
  await context.clearPermissions();
  await page.reload();
  await page.getByRole("button", { name: /use my location/i }).click();
  await expect(page.getByText(/Location permission was denied/i)).toBeVisible();
});

test("valid private upload succeeds, remains private, and can be removed", async ({ page, request }) => {
  const auth = await authenticatePage(page, request);
  await page.goto("/PhotoUpload");
  await page.locator('input[type="file"]').setInputFiles(sampleImage);
  await page.locator("#photo-title").fill("Private E2E upload");
  await page.locator("#photo-latitude").fill("4.815600");
  await page.locator("#photo-longitude").fill("7.049800");
  await page.locator('label[for="visibility-private"]').click();
  await expect(page.locator("#visibility-private")).toBeChecked();
  const uploadResponse = page.waitForResponse((response) =>
    response.url().includes("/api/upload/photo") && response.request().method() === "POST");
  await page.getByRole("button", { name: "Share photo" }).click();
  const response = await uploadResponse;
  expect(response.status()).toBe(201);
  const id = (await response.json()).id;
  await expect(page.getByText("Photo shared", { exact: true })).toBeVisible();
  const publicPhotos = await (await request.get("/api/photos")).json();
  expect(publicPhotos.some((photo) => photo.id === id)).toBeFalsy();
  expect((await request.get(`/api/photos/${id}`)).status()).toBe(404);
  expect((await request.get(`/api/photos/${id}/file`)).status()).toBe(404);
  const ownerDetail = await request.get(`/api/photos/${id}`, {
    headers: { Authorization: `Bearer ${auth.token}` },
  });
  expect(ownerDetail.status()).toBe(200);
  await deletePhoto(request, auth, id);
});

test("malformed image upload is rejected", async ({ page, request }) => {
  await authenticatePage(page, request);
  await page.goto("/PhotoUpload");
  await page.locator('input[type="file"]').setInputFiles({
    name: "not-an-image.jpg", mimeType: "image/jpeg", buffer: Buffer.from("not an image"),
  });
  await page.locator("#photo-title").fill("Invalid image");
  await page.locator("#photo-latitude").fill("4.815600");
  await page.locator("#photo-longitude").fill("7.049800");
  await page.getByRole("button", { name: "Share photo" }).click();
  await expect(page.getByRole("alert")).toContainText(/not a valid image|supported/i);
});

test("marker popup, search, category and my-photos filters work", async ({ page, request }) => {
  await authenticatePage(page, request);
  await page.goto("/MapRenderer");
  await expect(page.locator(".map-results__list")).toBeVisible();
  await page.locator("#photo-search").fill("Rumuola");
  await page.locator("#photo-search").press("Enter");
  await expect(page.locator(".map-results__list")).toContainText("Rumuola");
  await page.locator("#photo-category").selectOption("Infrastructure");
  await expect(page.locator(".map-results__list")).toContainText("Infrastructure");
  await page.locator('label[for="mine-filter"]').click();
  await expect(page.locator("#mine-filter")).toBeChecked();
  await expect(page.locator(".map-results__list")).toBeVisible();
  await page.locator(".result-card").first().click();
  await expect(page.locator(".leaflet-popup-content")).toBeVisible();
});

test("an owner can edit and delete while another user is forbidden", async ({ page, request }) => {
  const auth = await authenticatePage(page, request);
  const id = await createPhoto(request, auth, { title: "Editable E2E photo" });
  await page.goto(`/photos/${id}/edit`);
  await page.locator("#edit-title").fill("Edited E2E photo");
  await page.getByRole("button", { name: /save changes/i }).click();
  await expect(page).toHaveURL(new RegExp(`/photos/${id}$`));
  await expect(page.getByRole("heading", { name: "Edited E2E photo" })).toBeVisible();
  const registration = await request.post("/api/auth/register", {
    data: { email: `other-${Date.now()}@example.test`, password: "Testing123" },
  });
  expect(registration.ok()).toBeTruthy();
  const other = await registration.json();
  const denied = await request.put(`/api/photos/${id}`, {
    headers: { Authorization: `Bearer ${other.token}` },
    data: { title: "Not allowed", latitude: 4.8, longitude: 7.0 },
  });
  expect(denied.status()).toBe(403);
  await page.goto("/MyPhotos");
  const card = page.locator(".photo-card", { hasText: "Edited E2E photo" });
  await card.getByRole("button", { name: /delete/i }).click();
  await page.getByRole("button", { name: /delete photograph/i }).click();
  await expect(page.locator(".photo-card", { hasText: "Edited E2E photo" })).toHaveCount(0);
});

test("mobile and tablet layouts keep navigation and map usable", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  const toggle = page.getByRole("button", { name: /toggle navigation/i });
  await expect(toggle).toBeVisible();
  await toggle.click();
  await expect(page.getByRole("link", { name: /explore map/i }).first()).toBeVisible();
  await page.setViewportSize({ width: 768, height: 1024 });
  await page.goto("/MapRenderer");
  const dimensions = await page.locator(".map-canvas").boundingBox();
  expect(dimensions.width).toBeGreaterThan(300);
  expect(dimensions.height).toBeGreaterThan(300);
});

test("API failure and empty-state paths are explicit and key pages avoid console errors", async ({ page }) => {
  const errors = [];
  page.on("console", (message) => {
    if (message.type() === "error") errors.push(message.text());
  });
  await page.route("**/api/photos?**", (route) => route.abort("failed"));
  await page.goto("/MapRenderer");
  await expect(page.getByText(/could not|unable|try again/i).first()).toBeVisible();
  await page.unroute("**/api/photos?**");
  await page.route("**/api/photos?**", (route) => route.fulfill({
    status: 200, contentType: "application/json", body: "[]",
  }));
  await page.reload();
  await expect(page.getByText(/no photographs/i).first()).toBeVisible();
  expect(errors.filter((error) => !/ERR_FAILED|Failed to load resource/.test(error))).toEqual([]);
});
