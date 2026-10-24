export const DEFAULT_DECIMALS = 6;

export function toNumber(value) {
  if (value === null || value === undefined) return null;
  if (typeof value === "number") return Number.isFinite(value) ? value : null;
  const text = String(value).trim();
  if (text === "") return null;
  const parsed = Number(text);
  return Number.isFinite(parsed) ? parsed : null;
}

export function isValidLatitude(value) {
  const parsed = toNumber(value);
  return parsed !== null && parsed >= -90 && parsed <= 90;
}

export function isValidLongitude(value) {
  const parsed = toNumber(value);
  return parsed !== null && parsed >= -180 && parsed <= 180;
}

export function isValidCoordinatePair(latitude, longitude) {
  return isValidLatitude(latitude) && isValidLongitude(longitude);
}

export function formatCoordinate(value, decimals = DEFAULT_DECIMALS) {
  const parsed = toNumber(value);
  if (parsed === null) return "";
  return parsed.toFixed(decimals);
}

export function formatCoordinates(latitude, longitude, decimals = DEFAULT_DECIMALS) {
  if (!isValidCoordinatePair(latitude, longitude)) return "";
  return `${formatCoordinate(latitude, decimals)}, ${formatCoordinate(longitude, decimals)}`;
}

export function toLeafletLatLng(latitude, longitude) {
  if (!isValidCoordinatePair(latitude, longitude)) return null;
  return [Number(latitude), Number(longitude)];
}

export function toGeoJsonPosition(latitude, longitude) {
  if (!isValidCoordinatePair(latitude, longitude)) return null;
  return [Number(longitude), Number(latitude)];
}

export function openStreetMapUrl(latitude, longitude, zoom = 16) {
  if (!isValidCoordinatePair(latitude, longitude)) return "";
  return `https://www.openstreetmap.org/?mlat=${formatCoordinate(
    latitude
  )}&mlon=${formatCoordinate(longitude)}#map=${zoom}/${formatCoordinate(
    latitude
  )}/${formatCoordinate(longitude)}`;
}

export function validateCoordinates(latitude, longitude) {
  if (!isValidLatitude(latitude)) {
    return "Latitude must be a number between -90 and 90.";
  }
  if (!isValidLongitude(longitude)) {
    return "Longitude must be a number between -180 and 180.";
  }
  return "";
}

export function formatDisplayDate(value, options = {}) {
  if (!value) return "";
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    ...options,
  }).format(date);
}

export function formatDisplayDateTime(value) {
  if (!value) return "";
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function clamp(value, minimum, maximum) {
  return Math.min(Math.max(value, minimum), maximum);
}
