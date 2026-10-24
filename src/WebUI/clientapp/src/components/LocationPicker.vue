<template>
  <div class="location-picker">
    <div ref="mapEl" class="location-picker__map" :style="{ height }" role="application" :aria-label="ariaLabel" />
    <div v-if="!readonly" class="location-picker__hint">
      <i class="fa fa-hand-pointer-o mr-1" aria-hidden="true" />
      Click the map or drag the pin to set the location
    </div>
  </div>
</template>

<script>
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { formatCoordinates, isValidCoordinatePair, toLeafletLatLng } from "@/utils/geo";

const DEFAULT_CENTER = [4.8156, 7.0498];

export default {
  name: "LocationPicker",
  props: {
    latitude: { type: [Number, String], default: null },
    longitude: { type: [Number, String], default: null },
    defaultLatitude: { type: Number, default: DEFAULT_CENTER[0] },
    defaultLongitude: { type: Number, default: DEFAULT_CENTER[1] },
    defaultZoom: { type: Number, default: 12 },
    minZoom: { type: Number, default: 3 },
    maxZoom: { type: Number, default: 19 },
    layers: { type: Array, default: () => [] },
    height: { type: String, default: "320px" },
    readonly: { type: Boolean, default: false },
    panOnInitial: { type: Boolean, default: true },
    ariaLabel: { type: String, default: "Location picker map" },
  },
  data() {
    return {
      ready: false,
    };
  },
  watch: {
    latitude() {
      this.syncMarker();
    },
    longitude() {
      this.syncMarker();
    },
  },
  mounted() {
    this.$nextTick(this.initialise);
  },
  beforeDestroy() {
    if (this._resizeObserver) {
      this._resizeObserver.disconnect();
      this._resizeObserver = null;
    }
    window.removeEventListener("resize", this.invalidate);
    if (this._map) {
      this._map.remove();
      this._map = null;
    }
    this._marker = null;
  },
  methods: {
    initialise() {
      if (this._map || !this.$refs.mapEl) return;

      this._map = L.map(this.$refs.mapEl, {
        center: [this.defaultLatitude, this.defaultLongitude],
        zoom: this.defaultZoom,
        minZoom: this.minZoom,
        maxZoom: this.maxZoom,
        scrollWheelZoom: true,
        worldCopyJump: true,
      });

      this._baseLayers = {};
      const layers = this.layers && this.layers.length ? this.layers : [this.fallbackLayer()];
      let defaultLayer = null;

      layers.forEach((layer, index) => {
        const tileLayer = L.tileLayer(layer.urlTemplate, {
          attribution: layer.attribution || "",
          maxZoom: layer.maximumZoom || this.maxZoom,
        });
        this._baseLayers[layer.name || `Layer ${index + 1}`] = tileLayer;
        if (!defaultLayer || layer.isDefault || (!layer.isDefault && index === 0)) {
          defaultLayer = tileLayer;
        }
      });

      if (defaultLayer) defaultLayer.addTo(this._map);
      if (Object.keys(this._baseLayers).length > 1) {
        L.control.layers(this._baseLayers, {}, { position: "topright" }).addTo(this._map);
      }

      L.control.scale({ imperial: false, position: "bottomleft" }).addTo(this._map);

      this._map.on("click", (event) => {
        if (this.readonly) return;
        this._syncingFromMap = true;
        this.placeMarker(event.latlng.lat, event.latlng.lng, true);
        this.$emit("selected", { latitude: event.latlng.lat, longitude: event.latlng.lng });
        this.$nextTick(() => {
          this._syncingFromMap = false;
        });
      });

      this.syncMarker();
      this.ready = true;
      this.$emit("ready", this._map);

      setTimeout(() => {
        if (this._map) this._map.invalidateSize();
      }, 120);

      if (typeof ResizeObserver !== "undefined") {
        this._resizeObserver = new ResizeObserver(() => {
          if (this._map) this._map.invalidateSize();
        });
        this._resizeObserver.observe(this.$refs.mapEl);
      }

      window.addEventListener("resize", this.invalidate);
    },

    fallbackLayer() {
      return {
        name: "OpenStreetMap",
        urlTemplate: "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright" rel="noopener noreferrer">OpenStreetMap</a> contributors',
        maximumZoom: 19,
        isDefault: true,
      };
    },

    syncMarker() {
      if (!this._map) return;
      if (this._syncingFromMap) return;

      const point = toLeafletLatLng(this.latitude, this.longitude);
      const isInitialView = !this._initialViewDone;
      this._initialViewDone = true;

      if (!point) {
        this.removeMarker();
        return;
      }

      if (isInitialView && this.panOnInitial) {
        this._map.setView(point, Math.max(this.defaultZoom, 13));
      }

      this.placeMarker(point[0], point[1], false);
    },

    placeMarker(latitude, longitude, pan) {
      if (!this._map) return;
      const point = L.latLng(latitude, longitude);
      if (!this._marker) {
        const icon = L.divIcon({
          className: "location-pin-icon",
          html: '<span class="location-pin"></span>',
          iconSize: [34, 44],
          iconAnchor: [17, 42],
          popupAnchor: [0, -36],
        });
        this._marker = L.marker(point, {
          icon,
          draggable: !this.readonly,
          keyboard: !this.readonly,
          title: "Selected photo location",
        }).addTo(this._map);

        this._marker.bindPopup(
          `<strong>Selected location</strong><br/>${formatCoordinates(latitude, longitude)}`,
          { closeButton: false }
        );

        if (!this.readonly) {
          this._marker.on("dragend", () => {
            const position = this._marker.getLatLng();
            this._marker.setPopupContent(
              `<strong>Selected location</strong><br/>${formatCoordinates(
                position.lat,
                position.lng
              )}`
            );
            this.$emit("selected", {
              latitude: position.lat,
              longitude: position.lng,
            });
          });
        }
      } else {
        this._marker.setLatLng(point);
        this._marker.setPopupContent(
          `<strong>Selected location</strong><br/>${formatCoordinates(latitude, longitude)}`
        );
      }

      if (pan) {
        this._map.panTo(point);
      }
    },

    removeMarker() {
      if (this._marker && this._map) {
        this._map.removeLayer(this._marker);
        this._marker = null;
      }
    },

    panTo(latitude, longitude, zoom) {
      if (!this._map || !isValidCoordinatePair(latitude, longitude)) return;
      const targetZoom = zoom || Math.max(this._map.getZoom(), 14);
      this._map.flyTo([Number(latitude), Number(longitude)], targetZoom);
    },

    invalidate() {
      if (this._map) this._map.invalidateSize();
    },

    async requestFullscreen() {
      const element = this.$refs.mapEl;
      if (!element || !element.requestFullscreen) return;
      await element.requestFullscreen();
      setTimeout(this.invalidate, 150);
    },
  },
};
</script>

<style scoped>
.location-picker {
  position: relative;
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid #cbd5e1;
  background: #e2e8f0;
}

.location-picker__map {
  width: 100%;
  min-height: 240px;
  z-index: 0;
}

.location-picker__hint {
  position: absolute;
  left: 50%;
  bottom: 12px;
  transform: translateX(-50%);
  background: rgba(15, 23, 42, 0.86);
  color: #e2e8f0;
  padding: 0.35rem 0.7rem;
  border-radius: 999px;
  font-size: 0.76rem;
  z-index: 500;
  white-space: nowrap;
  pointer-events: none;
}
</style>

<style>
.location-pin-icon {
  background: transparent !important;
  border: none !important;
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
</style>
