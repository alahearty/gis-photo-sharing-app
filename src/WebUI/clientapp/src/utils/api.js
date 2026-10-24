const AUTH_KEY = "gis_photo_auth";

export class ApiError extends Error {
  constructor(message, status, data) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
  }
}

export function apiBase() {
  return process.env.VUE_APP_API_URL || "";
}

export function resolveMediaUrl(path) {
  if (!path) return "";
  if (/^https?:\/\//i.test(path) || path.startsWith("data:") || path.startsWith("blob:")) {
    return path;
  }
  const base = apiBase();
  return `${base}${path.startsWith("/") ? "" : "/"}${path}`;
}

export function getStoredToken() {
  try {
    const raw = localStorage.getItem(AUTH_KEY);
    if (!raw) return null;
    if (raw.startsWith("{")) {
      const parsed = JSON.parse(raw);
      return parsed && parsed.token ? parsed.token : null;
    }
    return raw;
  } catch {
    return null;
  }
}

export function buildQuery(params = {}) {
  const query = new URLSearchParams();
  Object.keys(params).forEach((key) => {
    const value = params[key];
    if (value === null || value === undefined || value === "") return;
    query.append(key, String(value));
  });
  const text = query.toString();
  return text ? `?${text}` : "";
}

function messageFromPayload(payload, fallback) {
  if (!payload) return fallback;
  if (typeof payload === "string") return payload;
  if (payload.error) return payload.error;
  if (payload.detail) return payload.detail;
  if (payload.title && payload.title !== "An error occurred while processing the request.") {
    return payload.title;
  }
  if (payload.errors && typeof payload.errors === "object") {
    const first = Object.keys(payload.errors)[0];
    if (first) {
      const values = payload.errors[first];
      return Array.isArray(values) ? values[0] : String(values);
    }
  }
  return fallback;
}

export async function apiRequest(path, options = {}) {
  const {
    method = "GET",
    body,
    headers = {},
    auth = true,
    signal,
    raw = false,
  } = options;

  const requestHeaders = { ...headers };
  let requestBody = body;

  if (body !== undefined && body !== null && !(body instanceof FormData)) {
    requestHeaders["Content-Type"] = "application/json";
    requestBody = JSON.stringify(body);
  }

  const token = auth ? getStoredToken() : null;
  if (token) requestHeaders.Authorization = `Bearer ${token}`;

  let response;
  try {
    response = await fetch(`${apiBase()}${path}`, {
      method,
      headers: requestHeaders,
      body: requestBody,
      signal,
    });
  } catch (error) {
    if (error.name === "AbortError") throw error;
    throw new ApiError(
      "Could not connect to the server. Check your connection and try again.",
      0,
      null
    );
  }

  if (raw) {
    if (!response.ok) {
      const data = await response.json().catch(() => null);
      throw new ApiError(
        messageFromPayload(data, `Request failed with status ${response.status}.`),
        response.status,
        data
      );
    }
    return response;
  }

  const contentType = response.headers.get("content-type") || "";
  const data = contentType.includes("application/json")
    ? await response.json().catch(() => null)
    : await response.text().catch(() => "");

  if (!response.ok) {
    throw new ApiError(
      messageFromPayload(data, `Request failed with status ${response.status}.`),
      response.status,
      data
    );
  }

  return data;
}

export const api = {
  get(path, params, options = {}) {
    return apiRequest(`${path}${buildQuery(params)}`, options);
  },
  post(path, body, options = {}) {
    return apiRequest(path, { ...options, method: "POST", body });
  },
  put(path, body, options = {}) {
    return apiRequest(path, { ...options, method: "PUT", body });
  },
  delete(path, options = {}) {
    return apiRequest(path, { ...options, method: "DELETE" });
  },
};
