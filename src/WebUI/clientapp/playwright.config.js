const path = require("path");
const { defineConfig, devices } = require("@playwright/test");

const baseURL = "http://127.0.0.1:5397";

module.exports = defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"] } },
  ],
  webServer: {
    command: "dotnet run --project ../WebUI.csproj --no-build --no-launch-profile",
    url: `${baseURL}/health`,
    timeout: 120000,
    reuseExistingServer: false,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      ASPNETCORE_URLS: baseURL,
      UseInMemoryDatabase: "true",
      DatabaseSeed__DefaultUser: "true",
      DatabaseSeed__SamplePhotos: "true",
      PhotoSettings__StorageRoot: path.resolve(__dirname, "../../../artifacts/e2e-storage"),
    },
  },
});
