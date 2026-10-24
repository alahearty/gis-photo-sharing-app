<template>
  <div class="map-page container-fluid px-2 px-lg-3">
    <div class="map-header card border-0 shadow-sm mb-3">
      <div class="card-body py-3">
        <div class="d-flex flex-column flex-lg-row align-items-lg-center justify-content-between">
          <div class="mb-2 mb-lg-0">
            <h1 class="h4 mb-1 font-weight-bold text-dark">Explore the photo map</h1>
            <p class="small text-muted mb-0">
              <i class="fa fa-map-marker text-primary mr-1" aria-hidden="true" />
              {{ settings.map.defaultLocationName }}
              <span class="text-muted">· {{ formatCoordinates(settings.map.defaultLatitude, settings.map.defaultLongitude) }}</span>
            </p>
          </div>
          <div class="map-toolbar" role="toolbar" aria-label="Map tools">
            <button type="button" class="btn btn-sm btn-outline-primary" @click="returnToPortHarcourt">
              <i class="fa fa-home mr-1" aria-hidden="true" /> Port Harcourt
            </button>
            <button type="button" class="btn btn-sm btn-outline-primary" :disabled="!photos.length" @click="fitToPhotos">
              <i class="fa fa-arrows-alt mr-1" aria-hidden="true" /> Fit photos
            </button>
            <button type="button" class="btn btn-sm btn-outline-primary" :disabled="locating" @click="locateMe">
              <i class="fa fa-crosshairs mr-1" aria-hidden="true" />
              {{ locating ? "Locating…" : "Locate me" }}
            </button>
            <button type="button" class="btn btn-sm btn-outline-primary" @click="toggleFullscreen">
              <i :class="['fa', fullscreen ? 'fa-compress' : 'fa-expand', 'mr-1']" aria-hidden="true" />
              {{ fullscreen ? "Exit full screen" : "Full screen" }}
            </button>
            <button
              type="button"
              class="btn btn-sm"
              :class="viewportFilter ? 'btn-primary' : 'btn-outline-primary'"
              @click="toggleViewportFilter"
            >
              <i class="fa fa-vector-square mr-1" aria-hidden="true" />
              {{ viewportFilter ? "Clear area" : "Search this area" }}
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary" @click="openGeoJson">
              <i class="fa fa-download mr-1" aria-hidden="true" /> GeoJSON
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-secondary d-lg-none"
              :aria-expanded="filtersOpen ? 'true' : 'false'"
              @click="filtersOpen = !filtersOpen"
            >
              <i class="fa fa-filter mr-1" aria-hidden="true" /> Filters
              <span v-if="activeFilterCount" class="badge badge-primary ml-1">{{ activeFilterCount }}</span>
            </button>
            <button
              type="button"
              class="btn btn-sm btn-outline-secondary"
              :aria-expanded="resultsVisible ? 'true' : 'false'"
              @click="toggleResults"
            >
              <i class="fa fa-list mr-1" aria-hidden="true" />
              {{ resultsVisible ? "Hide results" : "Show results" }}
            </button>
          </div>
        </div>

        <div class="map-filters" :class="{ 'map-filters--open': filtersOpen }">
          <div class="filter-field filter-field--search">
            <label for="photo-search">Search</label>
            <div class="input-group input-group-sm">
              <div class="input-group-prepend">
                <span class="input-group-text"><i class="fa fa-search" aria-hidden="true" /></span>
              </div>
              <input
                id="photo-search"
                v-model="filters.search"
                type="search"
                class="form-control"
                placeholder="Title, description or place"
                @input="onSearchInput"
                @keyup.enter="loadPhotos"
              />
            </div>
          </div>

          <div class="filter-field">
            <label for="photo-category">Category</label>
            <select id="photo-category" v-model="filters.category" class="form-control form-control-sm" @change="loadPhotos">
              <option value="">All categories</option>
              <option v-for="category in settings.photo.categories" :key="category" :value="category">
                {{ category }}
              </option>
            </select>
          </div>

          <div class="filter-field">
            <label for="photo-from">From</label>
            <input id="photo-from" v-model="filters.from" type="date" class="form-control form-control-sm" @change="loadPhotos" />
          </div>

          <div class="filter-field">
            <label for="photo-to">To</label>
            <input id="photo-to" v-model="filters.to" type="date" class="form-control form-control-sm" @change="loadPhotos" />
          </div>

          <div class="filter-field filter-field--mine">
            <label class="mb-1">Ownership</label>
            <div v-if="$store.getters.isLoggedIn" class="custom-control custom-checkbox">
              <input
                id="mine-filter"
                v-model="filters.mine"
                type="checkbox"
                class="custom-control-input"
                @change="loadPhotos"
              />
              <label class="custom-control-label small" for="mine-filter">My photos only</label>
            </div>
            <router-link v-else :to="{ name: 'Login', query: { redirect: $route.fullPath } }" class="small">
              Sign in to use My photos
            </router-link>
          </div>

          <div class="filter-field filter-field--actions">
            <button type="button" class="btn btn-sm btn-outline-secondary" @click="clearFilters">
              <i class="fa fa-times mr-1" aria-hidden="true" /> Clear
            </button>
          </div>
        </div>

        <div v-if="viewportFilter" class="filter-chip-row">
          <span class="badge badge-info">
            <i class="fa fa-vector-square mr-1" aria-hidden="true" /> Within the current map area
          </span>
          <button type="button" class="btn btn-link btn-sm p-0 ml-2" @click="toggleViewportFilter">Remove</button>
        </div>

        <div v-if="locationError" class="alert alert-warning py-2 px-3 small mt-2 mb-0" role="alert">
          {{ locationError }}
          <span v-if="locationNeedsHttps" class="d-block mt-1 text-muted">
            Browser location requires HTTPS or localhost.
          </span>
        </div>
      </div>
    </div>

    <div class="map-workspace" :class="{ 'map-workspace--results-hidden': !resultsVisible }">
      <aside v-if="resultsVisible" class="map-results" aria-label="Photo results">
        <div class="map-results__head">
          <div>
            <h2 class="h6 mb-0">Photographs</h2>
            <p class="small text-muted mb-0">
              {{ loading ? "Loading…" : `${photos.length} ${photos.length === 1 ? "result" : "results"}` }}
            </p>
          </div>
          <button type="button" class="btn btn-sm btn-link text-muted p-0" aria-label="Hide results" @click="toggleResults">
            <i class="fa fa-chevron-left" aria-hidden="true" />
          </button>
        </div>

        <div v-if="photos.length" class="map-results__list">
          <article
            v-for="photo in photos"
            :id="`photo-item-${photo.id}`"
            :key="photo.id"
            class="result-card"
            :class="{ 'result-card--active': selectedId === photo.id }"
            tabindex="0"
            role="button"
            :aria-label="`Show ${photo.title} on the map`"
            @click="selectPhoto(photo.id)"
            @keydown.enter.prevent="selectPhoto(photo.id)"
            @keydown.space.prevent="selectPhoto(photo.id)"
          >
            <div class="result-card__media">
              <img
                v-if="photo.thumbnailUrl || photo.imageUrl"
                :src="resolveMediaUrl(photo.thumbnailUrl || photo.imageUrl)"
                :alt="photo.title"
                loading="lazy"
                decoding="async"
                @error="onImageError"
              />
              <div v-else class="result-card__placeholder"><i class="fa fa-picture-o" aria-hidden="true" /></div>
            </div>
            <div class="result-card__body">
              <h3 class="h6 mb-1">{{ photo.title }}</h3>
              <p v-if="photo.locationName" class="small text-muted mb-1 text-truncate">
                <i class="fa fa-map-marker mr-1" aria-hidden="true" />{{ photo.locationName }}
              </p>
              <p class="small text-muted mb-2">
                <span v-if="photo.category" class="badge badge-light border mr-1">{{ photo.category }}</span>
                {{ formatDisplayDate(photo.createdAt) }}
              </p>
              <router-link
                :to="`/photos/${photo.id}`"
                class="small font-weight-bold"
                @click.stop
              >
                View details <span aria-hidden="true">→</span>
              </router-link>
            </div>
          </article>
        </div>
        <div v-else class="map-results__empty">
          <i class="fa fa-camera-retro fa-2x text-muted mb-2" aria-hidden="true" />
          <p class="small text-muted mb-1">
            {{ loading ? "Loading photographs…" : hasActiveFilters ? "No photographs match these filters." : "No photographs have been shared yet." }}
          </p>
          <button v-if="hasActiveFilters" type="button" class="btn btn-sm btn-outline-primary" @click="clearFilters">
            Clear filters
          </button>
          <router-link v-else-if="$store.getters.isLoggedIn" to="/PhotoUpload" class="btn btn-sm btn-primary">
            Share the first photo
          </router-link>
        </div>
      </aside>

      <div ref="mapStage" class="map-stage">
        <div ref="mapEl" class="map-canvas" role="application" aria-label="Interactive map of shared photographs" />

        <div v-if="loading" class="map-overlay-status" role="status">
          <span class="spinner-border spinner-border-sm text-primary mr-2" aria-hidden="true" />
          Loading photographs…
        </div>
        <div v-else-if="error" class="map-overlay-status map-overlay-status--error" role="alert">
          <p class="mb-2">{{ error }}</p>
          <button type="button" class="btn btn-sm btn-primary" @click="loadPhotos">Try again</button>
        </div>
        <div v-else-if="!photos.length" class="map-overlay-status">
          <p class="mb-2">
            {{ hasActiveFilters ? "No photographs match the current filters." : "No photographs have been shared yet." }}
          </p>
          <button v-if="hasActiveFilters" type="button" class="btn btn-sm btn-primary" @click="clearFilters">
            Clear filters
          </button>
          <router-link v-else-if="$store.getters.isLoggedIn" to="/PhotoUpload" class="btn btn-sm btn-primary">
            Share a photo
          </router-link>
        </div>

        <div v-if="pickMode" class="pick-panel" role="region" aria-label="Choose a coordinate">
          <div class="d-flex align-items-center justify-content-between mb-1">
            <strong class="small"><i class="fa fa-crosshairs mr-1" aria-hidden="true" />Pick a location</strong>
            <button type="button" class="btn btn-sm btn-link p-0" @click="cancelPickMode">Cancel</button>
          </div>
          <p class="small text-muted mb-2">Click or drag the pin on the map, then use the location for your photo.</p>
          <p v-if="pickSelection" class="small font-weight-bold mb-2">
            {{ formatCoordinates(pickSelection.latitude, pickSelection.longitude) }}
          </p>
          <div class="d-flex">
            <button
              type="button"
              class="btn btn-sm btn-primary mr-2"
              :disabled="!pickSelection"
              @click="usePickedLocation"
            >
              Use this location
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="!pickSelection" @click="copyPicked">
              Copy
            </button>
          </div>
        </div>

        <div v-if="pointer" class="map-pointer-coordinates" title="Click to copy coordinates" @click="copyPointer">
          <i class="fa fa-mouse-pointer mr-1" aria-hidden="true" />
          {{ formatCoordinates(pointer.latitude, pointer.longitude) }}
        </div>

        <div v-if="tileError" class="map-tile-warning" role="status">
          Some map tiles could not load. Check your connection.
        </div>

        <div v-if="toast" class="map-toast" role="status">{{ toast }}</div>
      </div>
    </div>
  </div>
</template>

<script>
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "leaflet.markercluster";
import "leaflet.markercluster/dist/MarkerCluster.css";
import "leaflet.markercluster/dist/MarkerCluster.Default.css";
import { api, apiRequest, buildQuery, resolveMediaUrl } from "@/utils/api";
import { DEFAULT_SETTINGS, loadSettings } from "@/utils/settings";
import {
  formatCoordinate,
  formatCoordinates,
  formatDisplayDate,
  isValidCoordinatePair,
  openStreetMapUrl,
} from "@/utils/geo";

function escapeHtml(value) {
  return String(value == null ? "" : value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

export default {
  name: "MapRenderer",
  data() {
    return {
      settings: DEFAULT_SETTINGS,
      photos: [],
      loading: true,
      error: null,
      filtersOpen: false,
      resultsVisible: true,
      filters: {
        search: "",
        category: "",
        from: "",
        to: "",
        mine: false,
      },
      viewportFilter: null,
      selectedId: null,
      pointer: null,
      tileError: false,
      pickMode: false,
      pickSelection: null,
      locating: false,
      locationError: "",
      locationNeedsHttps: false,
      fullscreen: false,
      toast: "",
    };
  },
  computed: {
    hasActiveFilters() {
      return !!(
        this.filters.search.trim() ||
        this.filters.category ||
        this.filters.from ||
        this.filters.to ||
        this.filters.mine ||
        this.viewportFilter
      );
    },
    activeFilterCount() {
      let count = 0;
      if (this.filters.search.trim()) count += 1;
      if (this.filters.category) count += 1;
      if (this.filters.from) count += 1;
      if (this.filters.to) count += 1;
      if (this.filters.mine) count += 1;
      if (this.viewportFilter) count += 1;
      return count;
    },
  },
  async mounted() {
    this._markersById = new Map();
    this._searchTimer = null;
    this._toastTimer = null;
    this._loadingController = null;
    this._deepLinkHandled = false;

    if (typeof window !== "undefined" && window.innerWidth < 992) {
      this.resultsVisible = false;
    }

    this.applyRouteQuery();
    this.settings = await loadSettings();
    this.$nextTick(() => {
      this.initialiseMap();
      this.loadPhotos();
    });
  },
  beforeDestroy() {
    if (this._searchTimer) clearTimeout(this._searchTimer);
    if (this._toastTimer) clearTimeout(this._toastTimer);
    if (this._pointerFrame) cancelAnimationFrame(this._pointerFrame);
    if (this._loadingController) this._loadingController.abort();
    if (this._resizeObserver) this._resizeObserver.disconnect();
    window.removeEventListener("resize", this.invalidateMap);
    document.removeEventListener("fullscreenchange", this.onFullscreenChange);

    if (this._cluster) {
      this._cluster.clearLayers();
      this._cluster = null;
    }
    if (this._map) {
      this._map.remove();
      this._map = null;
    }
    if (window.__GIS_PHOTOSHARE_MAP__) {
      delete window.__GIS_PHOTOSHARE_MAP__;
    }
    this._markersById = null;
  },
  methods: {
    resolveMediaUrl,
    formatCoordinates,
    formatDisplayDate,

    applyRouteQuery() {
      const query = this.$route.query || {};
      if (query.mine === "1" && this.$store.getters.isLoggedIn) {
        this.filters.mine = true;
      }
      if (query.pick === "1") {
        this.pickMode = true;
      }
    },

    initialiseMap() {
      if (this._map || !this.$refs.mapEl) return;

      const mapSettings = this.settings.map;
      this._map = L.map(this.$refs.mapEl, {
        center: [mapSettings.defaultLatitude, mapSettings.defaultLongitude],
        zoom: mapSettings.defaultZoom,
        minZoom: mapSettings.minimumZoom,
        maxZoom: mapSettings.maximumZoom,
        worldCopyJump: true,
        scrollWheelZoom: true,
      });

      // Exposed for automated browser verification and operator diagnostics.
      window.__GIS_PHOTOSHARE_MAP__ = this._map;

      this._baseLayers = {};
      const layers = mapSettings.layers && mapSettings.layers.length
        ? mapSettings.layers
        : DEFAULT_SETTINGS.map.layers;
      let defaultLayer = null;

      layers.forEach((layer, index) => {
        const tileLayer = L.tileLayer(layer.urlTemplate, {
          attribution: layer.attribution || "",
          maxZoom: layer.maximumZoom || mapSettings.maximumZoom,
          subdomains: ["a", "b", "c"],
        });
        tileLayer.on("tileerror", this.onTileError);
        this._baseLayers[layer.name || `Layer ${index + 1}`] = tileLayer;
        if (!defaultLayer || layer.isDefault) defaultLayer = tileLayer;
      });

      if (defaultLayer) defaultLayer.addTo(this._map);

      if (Object.keys(this._baseLayers).length > 1) {
        L.control.layers(this._baseLayers, {}, { position: "topright" }).addTo(this._map);
      }

      L.control.scale({ imperial: false, position: "bottomleft" }).addTo(this._map);

      this._cluster = L.markerClusterGroup({
        showCoverageOnHover: false,
        spiderfyOnMaxZoom: true,
        disableClusteringAtZoom: 18,
        maxClusterRadius: 55,
        chunkedLoading: true,
        iconCreateFunction: (cluster) => {
          const count = cluster.getChildCount();
          const size = count < 10 ? 40 : count < 100 ? 48 : 56;
          return L.divIcon({
            html: `<div class="photo-cluster"><span>${count}</span></div>`,
            className: "photo-cluster-icon",
            iconSize: [size, size],
          });
        },
      });
      this._map.addLayer(this._cluster);

      this._map.on("mousemove", this.onPointerMove);
      this._map.on("mouseout", this.onPointerOut);
      this._map.on("click", this.onMapClick);

      this._resizeObserver = typeof ResizeObserver !== "undefined"
        ? new ResizeObserver(() => this.invalidateMap())
        : null;
      if (this._resizeObserver) this._resizeObserver.observe(this.$refs.mapEl);

      window.addEventListener("resize", this.invalidateMap);
      document.addEventListener("fullscreenchange", this.onFullscreenChange);

      setTimeout(() => this.invalidateMap(), 120);
    },

    invalidateMap() {
      if (this._map) this._map.invalidateSize();
    },

    onTileError() {
      if (!this.tileError) this.tileError = true;
    },

    onPointerMove(event) {
      this._pendingPointer = event.latlng;
      if (this._pointerFrame) return;
      this._pointerFrame = requestAnimationFrame(() => {
        if (this._pendingPointer) {
          this.pointer = {
            latitude: this._pendingPointer.lat,
            longitude: this._pendingPointer.lng,
          };
        }
        this._pointerFrame = null;
      });
    },

    onPointerOut() {
      this.pointer = null;
    },

    onMapClick(event) {
      if (!this.pickMode) return;
      this.setPickLocation(event.latlng.lat, event.latlng.lng);
    },

    setPickLocation(latitude, longitude) {
      this.pickSelection = { latitude, longitude };

      if (!this._pickMarker) {
        const icon = L.divIcon({
          className: "location-pin-icon",
          html: '<span class="location-pin"></span>',
          iconSize: [34, 44],
          iconAnchor: [17, 42],
        });
        this._pickMarker = L.marker([latitude, longitude], {
          icon,
          draggable: true,
          keyboard: true,
          title: "Picked location",
        }).addTo(this._map);

        this._pickMarker.bindTooltip("Drag to adjust the picked location", {
          direction: "top",
          offset: [0, -36],
        });

        this._pickMarker.on("dragend", () => {
          const position = this._pickMarker.getLatLng();
          this.pickSelection = {
            latitude: position.lat,
            longitude: position.lng,
          };
        });
      } else {
        this._pickMarker.setLatLng([latitude, longitude]);
      }
    },

    cancelPickMode() {
      this.pickMode = false;
      this.pickSelection = null;
      if (this._pickMarker && this._map) {
        this._map.removeLayer(this._pickMarker);
        this._pickMarker = null;
      }
      const query = { ...this.$route.query };
      delete query.pick;
      this.$router.replace({ path: this.$route.path, query });
    },

    usePickedLocation() {
      if (!this.pickSelection) return;
      this.$router.push({
        path: "/PhotoUpload",
        query: {
          lat: formatCoordinate(this.pickSelection.latitude),
          lng: formatCoordinate(this.pickSelection.longitude),
        },
      });
    },

    copyPicked() {
      if (!this.pickSelection) return;
      this.copyText(
        formatCoordinates(this.pickSelection.latitude, this.pickSelection.longitude),
        "Picked coordinates copied."
      );
    },

    copyPointer() {
      if (!this.pointer) return;
      this.copyText(
        formatCoordinates(this.pointer.latitude, this.pointer.longitude),
        "Coordinates copied."
      );
    },

    async copyText(text, message) {
      try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
          await navigator.clipboard.writeText(text);
        } else {
          const area = document.createElement("textarea");
          area.value = text;
          area.setAttribute("readonly", "");
          area.style.position = "absolute";
          area.style.left = "-9999px";
          document.body.appendChild(area);
          area.select();
          document.execCommand("copy");
          document.body.removeChild(area);
        }
        this.showToast(message);
      } catch {
        this.showToast("Could not copy the coordinates.");
      }
    },

    showToast(message) {
      this.toast = message;
      if (this._toastTimer) clearTimeout(this._toastTimer);
      this._toastTimer = setTimeout(() => {
        this.toast = "";
      }, 3200);
    },

    onSearchInput() {
      if (this._searchTimer) clearTimeout(this._searchTimer);
      this._searchTimer = setTimeout(() => this.loadPhotos(), 350);
    },

    buildFilterParams() {
      const params = {
        search: this.filters.search.trim() || null,
        category: this.filters.category || null,
        from: this.filters.from || null,
        to: this.filters.to || null,
        mine: this.filters.mine ? "true" : null,
        sort: "newest",
      };

      if (this.viewportFilter) {
        params.minLat = this.viewportFilter.minLat;
        params.maxLat = this.viewportFilter.maxLat;
        params.minLng = this.viewportFilter.minLng;
        params.maxLng = this.viewportFilter.maxLng;
      }

      return params;
    },

    async loadPhotos() {
      if (this.filters.mine && !this.$store.getters.isLoggedIn) {
        this.filters.mine = false;
      }

      if (this._loadingController) this._loadingController.abort();
      this._loadingController = typeof AbortController !== "undefined" ? new AbortController() : null;

      this.loading = true;
      this.error = null;

      try {
        const photos = await api.get("/api/photos", this.buildFilterParams(), {
          signal: this._loadingController ? this._loadingController.signal : undefined,
        });

        this.photos = Array.isArray(photos)
          ? photos.filter((photo) => isValidCoordinatePair(photo.latitude, photo.longitude))
          : [];
        this.renderMarkers();
        this.loading = false;
        this.handleDeepLink();

        if (this.filters.mine && !this.photos.length) {
          this.showToast("You have not shared any photos that match the filters.");
        }
      } catch (error) {
        if (error.name === "AbortError") return;
        this.loading = false;
        this.error =
          error.status === 401
            ? "Your session has expired. Sign in again to view your photos."
            : error.message || "Could not load photographs.";
      }
    },

    renderMarkers() {
      if (!this._map || !this._cluster) return;

      this._cluster.clearLayers();
      this._markersById.clear();

      const base = this.$router.options.base || "/";
      const basePath = base.endsWith("/") ? base : `${base}/`;

      this.photos.forEach((photo) => {
        const icon = L.divIcon({
          className: "photo-marker-icon",
          html: '<span class="photo-marker-pin"><i class="fa fa-camera"></i></span>',
          iconSize: [30, 36],
          iconAnchor: [15, 34],
          popupAnchor: [0, -30],
        });

        const marker = L.marker([photo.latitude, photo.longitude], {
          icon,
          title: photo.title,
          keyboard: true,
          riseOnHover: true,
        });

        marker.bindPopup(this.popupHtml(photo, basePath), {
          maxWidth: 300,
          minWidth: 240,
          autoPan: true,
          className: "photo-popup",
        });

        marker.on("click", () => {
          this.selectPhoto(photo.id, { fromMarker: true });
        });

        this._markersById.set(photo.id, marker);
        this._cluster.addLayer(marker);
      });

      if (this.selectedId && !this._markersById.has(this.selectedId)) {
        this.selectedId = null;
      }
    },

    popupHtml(photo, basePath) {
      const image = resolveMediaUrl(photo.thumbnailUrl || photo.imageUrl);
      const imageHtml = image
        ? `<img class="photo-popup__image" src="${escapeHtml(image)}" alt="${escapeHtml(photo.title)}" loading="lazy" onerror="this.style.display='none'" />`
        : "";
      const location = photo.locationName
        ? `<div class="photo-popup__meta"><i class="fa fa-map-marker"></i>${escapeHtml(photo.locationName)}</div>`
        : "";
      const category = photo.category
        ? `<span class="photo-popup__category">${escapeHtml(photo.category)}</span>`
        : "";
      const description = photo.description
        ? `<p class="photo-popup__description">${escapeHtml(photo.description)}</p>`
        : "";

      return `
        <div class="photo-popup__content">
          ${imageHtml}
          <strong class="photo-popup__title">${escapeHtml(photo.title)}</strong>
          ${description}
          ${location}
          <div class="photo-popup__meta">
            ${category}
            <span>${escapeHtml(formatDisplayDate(photo.createdAt))}</span>
          </div>
          <a class="photo-popup__link" href="${escapeHtml(basePath)}photos/${photo.id}">View details →</a>
        </div>`;
    },

    handleDeepLink() {
      if (this._deepLinkHandled) return;
      const requestedId = Number(this.$route.query.photo);
      if (!requestedId) {
        this._deepLinkHandled = true;
        return;
      }
      this._deepLinkHandled = true;
      const photo = this.photos.find((item) => item.id === requestedId);
      if (photo) {
        this.$nextTick(() => this.selectPhoto(photo.id));
      } else {
        this.showToast("The linked photograph is not available with the current filters.");
      }
    },

    selectPhoto(id, options = {}) {
      const { fromMarker = false } = options;
      this.selectedId = id;
      const marker = this._markersById.get(id);

      if (marker && this._cluster) {
        this._cluster.zoomToShowLayer(marker, () => {
          if (!fromMarker) marker.openPopup();
          this.markSelectedMarker(id);
        });
      }

      this.$nextTick(() => {
        const element = document.getElementById(`photo-item-${id}`);
        if (element) element.scrollIntoView({ block: "nearest", behavior: "smooth" });
      });
    },

    markSelectedMarker(id) {
      if (!this._markersById) return;
      this._markersById.forEach((marker, markerId) => {
        const element = marker.getElement();
        if (!element) return;
        element.classList.toggle("is-selected", markerId === id);
      });
    },

    toggleResults() {
      this.resultsVisible = !this.resultsVisible;
      this.$nextTick(() => setTimeout(() => this.invalidateMap(), 80));
    },

    returnToPortHarcourt() {
      if (!this._map) return;
      const mapSettings = this.settings.map;
      this._map.flyTo(
        [mapSettings.defaultLatitude, mapSettings.defaultLongitude],
        mapSettings.defaultZoom
      );
    },

    fitToPhotos() {
      if (!this._map) return;
      const points = this.photos
        .filter((photo) => isValidCoordinatePair(photo.latitude, photo.longitude))
        .map((photo) => [photo.latitude, photo.longitude]);

      if (!points.length) {
        this.showToast("There are no photographs to fit.");
        return;
      }

      if (points.length === 1) {
        this._map.flyTo(points[0], 15);
        return;
      }

      const bounds = L.latLngBounds(points);
      this._map.fitBounds(bounds, { padding: [60, 60], maxZoom: 15 });
    },

    toggleViewportFilter() {
      if (this.viewportFilter) {
        this.viewportFilter = null;
        this.loadPhotos();
        return;
      }

      if (!this._map) return;
      const bounds = this._map.getBounds();
      this.viewportFilter = {
        minLat: bounds.getSouth(),
        maxLat: bounds.getNorth(),
        minLng: bounds.getWest(),
        maxLng: bounds.getEast(),
      };
      this.loadPhotos();
    },

    locateMe() {
      this.locationError = "";
      this.locationNeedsHttps = false;

      if (!navigator.geolocation) {
        this.locationError = "Geolocation is not supported by this browser. Pick a point on the map instead.";
        return;
      }

      this.locating = true;
      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.locating = false;
          const { latitude, longitude, accuracy } = position.coords;
          this.showLocation(latitude, longitude, accuracy);
          this.showToast(
            accuracy ? `Your location was found within about ${Math.round(accuracy)} metres.` : "Your location was found."
          );
        },
        (error) => {
          this.locating = false;
          if (error.code === 1) {
            this.locationError = "Location permission was denied. Pick a point on the map instead.";
            this.locationNeedsHttps = window.location.protocol !== "https:" && location.hostname !== "localhost";
          } else if (error.code === 2) {
            this.locationError = "Your location is currently unavailable. Pick a point on the map instead.";
          } else {
            this.locationError = "Finding your location timed out. Try again or pick a point on the map.";
          }
        },
        { enableHighAccuracy: true, timeout: 12000, maximumAge: 0 }
      );
    },

    showLocation(latitude, longitude, accuracy) {
      if (!this._map) return;

      if (!this._locationMarker) {
        const icon = L.divIcon({
          className: "location-pin-icon",
          html: '<span class="location-pin location-pin--me"></span>',
          iconSize: [34, 44],
          iconAnchor: [17, 42],
        });
        this._locationMarker = L.marker([latitude, longitude], { icon, title: "Your location" }).addTo(this._map);
        this._locationAccuracy = L.circle([latitude, longitude], {
          radius: accuracy || 30,
          color: "#0ea5e9",
          weight: 1,
          fillColor: "#0ea5e9",
          fillOpacity: 0.12,
          interactive: false,
        }).addTo(this._map);
      } else {
        this._locationMarker.setLatLng([latitude, longitude]);
        if (this._locationAccuracy) this._locationAccuracy.setLatLng([latitude, longitude]).setRadius(accuracy || 30);
      }

      this._map.flyTo([latitude, longitude], Math.max(14, this._map.getZoom()));
    },

    toggleFullscreen() {
      if (!document.fullscreenElement) {
        const element = this.$refs.mapStage;
        if (element && element.requestFullscreen) {
          element.requestFullscreen().catch(() => {
            this.showToast("Full screen mode is not available in this browser.");
          });
        }
      } else {
        document.exitFullscreen();
      }
    },

    onFullscreenChange() {
      this.fullscreen = document.fullscreenElement === this.$refs.mapStage;
      setTimeout(() => this.invalidateMap(), 160);
    },

    clearFilters() {
      this.filters = { search: "", category: "", from: "", to: "", mine: false };
      this.viewportFilter = null;
      this.loadPhotos();
    },

    async openGeoJson() {
      const params = {
        search: this.filters.search.trim() || null,
        category: this.filters.category || null,
        from: this.filters.from || null,
        to: this.filters.to || null,
      };
      if (this.viewportFilter) {
        params.minLat = this.viewportFilter.minLat;
        params.maxLat = this.viewportFilter.maxLat;
        params.minLng = this.viewportFilter.minLng;
        params.maxLng = this.viewportFilter.maxLng;
      }

      try {
        const response = await apiRequest(`/api/photos/geojson${buildQuery(params)}`, {
          raw: true,
          auth: false,
        });
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = "gis-photoshare.geojson";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        this.showToast("GeoJSON export started.");
      } catch (error) {
        this.showToast(error.message || "Could not export GeoJSON.");
      }
    },

    onImageError(event) {
      event.target.style.display = "none";
    },

    openStreetMapUrl,
  },
};
</script>

<style scoped>
.map-page {
  max-width: 1600px;
  margin: 0 auto;
}

.map-header {
  border-radius: 12px;
}

.map-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  justify-content: flex-start;
}

@media (min-width: 992px) {
  .map-toolbar {
    justify-content: flex-end;
  }
}

.map-filters {
  display: none;
  grid-template-columns: minmax(170px, 1.6fr) repeat(3, minmax(115px, 1fr)) minmax(130px, 1fr) auto;
  gap: 0.6rem;
  align-items: end;
  margin-top: 0.85rem;
  padding-top: 0.85rem;
  border-top: 1px solid #e2e8f0;
}

.map-filters--open {
  display: grid;
}

@media (min-width: 992px) {
  .map-filters {
    display: grid;
  }
}

.filter-field label {
  display: block;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #64748b;
  margin-bottom: 0.3rem;
}

.filter-field--mine {
  min-width: 140px;
}

.filter-field--actions {
  padding-bottom: 0.1rem;
}

.filter-chip-row {
  margin-top: 0.6rem;
}

.map-workspace {
  display: flex;
  gap: 0.85rem;
  height: clamp(520px, calc(100dvh - 245px), 920px);
  min-height: 520px;
}

.map-results {
  width: 330px;
  flex: 0 0 330px;
  display: flex;
  flex-direction: column;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
  overflow: hidden;
}

.map-results__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 0.85rem 0.95rem;
  border-bottom: 1px solid #e2e8f0;
}

.map-results__list {
  overflow-y: auto;
  padding: 0.6rem;
  flex: 1;
}

.map-results__empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 2rem 1rem;
}

.result-card {
  display: flex;
  gap: 0.7rem;
  padding: 0.55rem;
  border-radius: 10px;
  border: 1px solid transparent;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;
}

.result-card + .result-card {
  margin-top: 0.25rem;
}

.result-card:hover,
.result-card:focus {
  background: #f8fafc;
  border-color: #e2e8f0;
  outline: none;
}

.result-card--active {
  background: #eff6ff;
  border-color: #93c5fd;
}

.result-card__media {
  width: 74px;
  height: 64px;
  flex: 0 0 74px;
  border-radius: 8px;
  overflow: hidden;
  background: #e2e8f0;
}

.result-card__media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.result-card__placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #94a3b8;
}

.result-card__body {
  min-width: 0;
}

.map-stage {
  position: relative;
  flex: 1;
  min-width: 0;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 8px 28px rgba(15, 23, 42, 0.1);
  background: #e2e8f0;
}

.map-canvas {
  width: 100%;
  height: 100%;
  min-height: 420px;
  z-index: 0;
}

.map-overlay-status {
  position: absolute;
  top: 14px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(255, 255, 255, 0.97);
  color: #334155;
  padding: 0.65rem 0.9rem;
  border-radius: 10px;
  box-shadow: 0 6px 20px rgba(15, 23, 42, 0.16);
  font-size: 0.85rem;
  z-index: 600;
  max-width: calc(100% - 30px);
  text-align: center;
}

.map-overlay-status--error {
  border-left: 4px solid #dc2626;
}

.pick-panel {
  position: absolute;
  top: 72px;
  left: 14px;
  width: min(300px, calc(100% - 28px));
  background: #fff;
  border-radius: 10px;
  padding: 0.8rem 0.9rem;
  box-shadow: 0 8px 26px rgba(15, 23, 42, 0.2);
  z-index: 650;
}

.map-pointer-coordinates {
  position: absolute;
  right: 12px;
  bottom: 28px;
  background: rgba(15, 23, 42, 0.8);
  color: #e2e8f0;
  font-size: 0.72rem;
  font-variant-numeric: tabular-nums;
  padding: 0.22rem 0.5rem;
  border-radius: 6px;
  z-index: 600;
  cursor: pointer;
}

.map-tile-warning {
  position: absolute;
  left: 12px;
  bottom: 28px;
  background: #fffbeb;
  color: #92400e;
  border: 1px solid #fcd34d;
  padding: 0.35rem 0.6rem;
  border-radius: 8px;
  font-size: 0.75rem;
  z-index: 600;
  max-width: calc(100% - 250px);
}

.map-toast {
  position: absolute;
  right: 14px;
  top: 14px;
  background: #0f172a;
  color: #e2e8f0;
  padding: 0.45rem 0.75rem;
  border-radius: 8px;
  font-size: 0.8rem;
  z-index: 700;
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.25);
}

.map-stage:fullscreen {
  border-radius: 0;
  height: 100vh;
}

.map-stage:fullscreen .map-canvas {
  height: 100vh;
}

@media (max-width: 991.98px) {
  .map-workspace {
    display: block;
    height: auto;
    min-height: 0;
  }

  .map-stage {
    height: 66vh;
    min-height: 420px;
  }

  .map-results {
    position: fixed;
    left: 0;
    right: 0;
    bottom: 0;
    top: auto;
    width: 100%;
    height: 55vh;
    max-height: 520px;
    z-index: 1500;
    border-radius: 16px 16px 0 0;
    box-shadow: 0 -12px 34px rgba(15, 23, 42, 0.24);
  }

  .map-results__list {
    padding-bottom: 1.5rem;
  }

  .map-pointer-coordinates {
    bottom: 26px;
  }
}
</style>

<style>
.photo-marker-icon,
.photo-cluster-icon {
  background: transparent !important;
  border: none !important;
}

.photo-marker-pin {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  background: linear-gradient(145deg, #0ea5e9, #1d4ed8);
  border: 3px solid #fff;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.35);
  color: #fff;
  font-size: 12px;
}

.photo-marker-pin i {
  transform: rotate(45deg);
}

.leaflet-marker-icon.is-selected .photo-marker-pin {
  background: linear-gradient(145deg, #f59e0b, #dc2626);
  box-shadow: 0 0 0 5px rgba(245, 158, 11, 0.35), 0 5px 14px rgba(15, 23, 42, 0.35);
  transform: rotate(-45deg) scale(1.12);
}

.photo-cluster {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background: radial-gradient(circle at 30% 30%, #38bdf8, #1d4ed8);
  border: 3px solid #fff;
  color: #fff;
  font-weight: 700;
  font-size: 0.82rem;
  box-shadow: 0 6px 18px rgba(15, 23, 42, 0.3);
}

.photo-popup .leaflet-popup-content-wrapper {
  border-radius: 12px;
  padding: 0;
  overflow: hidden;
}

.photo-popup .leaflet-popup-content {
  margin: 0;
  width: auto !important;
}

.photo-popup__content {
  padding: 0.75rem;
}

.photo-popup__image {
  width: 100%;
  max-height: 160px;
  object-fit: cover;
  border-radius: 8px;
  margin-bottom: 0.5rem;
  display: block;
}

.photo-popup__title {
  display: block;
  font-size: 0.95rem;
  color: #0f172a;
  margin-bottom: 0.2rem;
}

.photo-popup__description {
  font-size: 0.78rem;
  color: #475569;
  margin: 0 0 0.35rem;
  line-height: 1.4;
}

.photo-popup__meta {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  flex-wrap: wrap;
  font-size: 0.74rem;
  color: #64748b;
  margin-bottom: 0.3rem;
}

.photo-popup__category {
  background: #e0f2fe;
  color: #0369a1;
  border-radius: 999px;
  padding: 0.05rem 0.4rem;
  font-size: 0.68rem;
  font-weight: 600;
}

.photo-popup__link {
  display: inline-block;
  margin-top: 0.2rem;
  font-size: 0.8rem;
  font-weight: 700;
  color: #0369a1;
}

.location-pin {
  display: block;
  width: 24px;
  height: 24px;
  margin: 6px auto 0;
  border-radius: 50% 50% 50% 0;
  transform: rotate(-45deg);
  background: linear-gradient(145deg, #0ea5e9, #1d4ed8);
  border: 3px solid #fff;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.35);
}

.location-pin--me {
  background: linear-gradient(145deg, #22c55e, #15803d);
}

.leaflet-container {
  font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
}
</style>
