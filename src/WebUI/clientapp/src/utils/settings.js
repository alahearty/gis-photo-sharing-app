import { api } from "@/utils/api";

export const DEFAULT_SETTINGS = {
  map: {
    defaultLatitude: 4.8156,
    defaultLongitude: 7.0498,
    defaultZoom: 12,
    minimumZoom: 3,
    maximumZoom: 19,
    defaultLocationName: "Port Harcourt, Rivers State, Nigeria",
    layers: [
      {
        name: "OpenStreetMap",
        urlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright" rel="noopener noreferrer">OpenStreetMap</a> contributors',
        maximumZoom: 19,
        isDefault: true,
      },
    ],
  },
  photo: {
    maxUploadBytes: 10 * 1024 * 1024,
    maxImagePixels: 50000000,
    maxImageDimension: 12000,
    thumbnailSize: 360,
    categories: [
      "General",
      "Landscape",
      "Buildings",
      "Roads",
      "Environment",
      "Tourism",
      "Infrastructure",
      "Other",
    ],
    allowedContentTypes: ["image/jpeg", "image/png", "image/gif", "image/webp"],
  },
  geocoding: {
    enabled: true,
    attribution: "Geocoding © OpenStreetMap contributors (Nominatim)",
  },
};

let settingsPromise = null;

function mergeSettings(remote) {
  if (!remote) return DEFAULT_SETTINGS;
  return {
    map: { ...DEFAULT_SETTINGS.map, ...(remote.map || {}) },
    photo: { ...DEFAULT_SETTINGS.photo, ...(remote.photo || {}) },
    geocoding: { ...DEFAULT_SETTINGS.geocoding, ...(remote.geocoding || {}) },
  };
}

export function loadSettings() {
  if (!settingsPromise) {
    settingsPromise = api
      .get("/api/settings")
      .then((settings) => mergeSettings(settings))
      .catch(() => DEFAULT_SETTINGS);
  }
  return settingsPromise;
}

export function resetSettingsCache() {
  settingsPromise = null;
}
