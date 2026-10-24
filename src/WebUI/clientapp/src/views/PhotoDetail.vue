<template>
  <div class="photo-detail container py-3 py-lg-4">
    <div v-if="loading" class="text-center py-5 text-muted">
      <span class="spinner-border spinner-border-sm mr-2" aria-hidden="true" /> Loading photograph…
    </div>

    <div v-else-if="!photo" class="empty-state card border-0 shadow-sm">
      <div class="card-body p-5 text-center">
        <i class="fa fa-camera-retro fa-2x text-muted mb-3" aria-hidden="true" />
        <h1 class="h4">Photograph not found</h1>
        <p class="text-muted">It may have been removed, made private, or the link may be incorrect.</p>
        <router-link to="/MapRenderer" class="btn btn-primary mr-2">Explore the map</router-link>
        <router-link to="/MyPhotos" class="btn btn-outline-primary">My photos</router-link>
      </div>
    </div>

    <template v-else>
      <div class="d-flex flex-wrap justify-content-between align-items-center mb-3">
        <router-link to="/MapRenderer" class="btn btn-sm btn-outline-secondary">
          <i class="fa fa-arrow-left mr-1" aria-hidden="true" /> Back to map
        </router-link>
        <div v-if="photo.isOwner" class="d-flex flex-wrap">
          <router-link :to="`/photos/${photo.id}/edit`" class="btn btn-sm btn-primary mr-2">
            <i class="fa fa-pencil mr-1" aria-hidden="true" /> Edit
          </router-link>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="showDeleteConfirm = true">
            <i class="fa fa-trash mr-1" aria-hidden="true" /> Delete
          </button>
        </div>
      </div>

      <div class="card border-0 shadow-sm overflow-hidden mb-4">
        <div class="row no-gutters">
          <div class="col-lg-7 detail-media">
            <button
              v-if="photo.imageUrl && !imageFailed"
              type="button"
              class="detail-media__button"
              aria-label="Open full-size photograph in a new tab"
              @click="openImage"
            >
              <img :src="resolveMediaUrl(photo.imageUrl)" :alt="photo.title" class="detail-image" @error="imageFailed = true" />
            </button>
            <div v-else class="detail-placeholder">
              <i class="fa fa-picture-o fa-3x mb-2" aria-hidden="true" />
              <span class="small">Image unavailable</span>
            </div>
          </div>
          <div class="col-lg-5">
            <div class="detail-body p-4 p-lg-5">
              <div class="d-flex flex-wrap align-items-center mb-2">
                <span v-if="photo.category" class="badge badge-info mr-2">{{ photo.category }}</span>
                <span v-if="photo.isOwner" class="badge" :class="photo.isPublic ? 'badge-success' : 'badge-secondary'">
                  <i :class="['fa', photo.isPublic ? 'fa-globe' : 'fa-lock', 'mr-1']" aria-hidden="true" />
                  {{ photo.isPublic ? "Public" : "Only me" }}
                </span>
              </div>

              <h1 class="detail-title mb-3">{{ photo.title }}</h1>
              <p v-if="photo.description" class="detail-description text-muted">{{ photo.description }}</p>

              <dl class="detail-meta">
                <dt><i class="fa fa-map-marker" aria-hidden="true" /> Location</dt>
                <dd>
                  <span v-if="photo.locationName" class="font-weight-bold d-block">{{ photo.locationName }}</span>
                  <span class="coordinate-text">{{ formatCoordinates(photo.latitude, photo.longitude) }}</span>
                  <span class="d-flex flex-wrap mt-2">
                    <button type="button" class="btn btn-sm btn-outline-secondary mr-2 mb-1" @click="copyCoordinates">
                      <i class="fa fa-copy mr-1" aria-hidden="true" /> Copy coordinates
                    </button>
                    <a
                      :href="osmUrl"
                      class="btn btn-sm btn-outline-secondary mr-2 mb-1"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      <i class="fa fa-external-link mr-1" aria-hidden="true" /> Open in OpenStreetMap
                    </a>
                    <router-link :to="`/MapRenderer?photo=${photo.id}`" class="btn btn-sm btn-outline-primary mb-1">
                      <i class="fa fa-map-o mr-1" aria-hidden="true" /> Show on map
                    </router-link>
                  </span>
                </dd>

                <dt><i class="fa fa-calendar" aria-hidden="true" /> Uploaded</dt>
                <dd>{{ formatDisplayDateTime(photo.createdAt) }}</dd>

                <template v-if="photo.takenAt">
                  <dt><i class="fa fa-camera" aria-hidden="true" /> Captured</dt>
                  <dd>{{ formatDisplayDate(photo.takenAt) }}</dd>
                </template>

                <template v-if="photo.updatedAt">
                  <dt><i class="fa fa-refresh" aria-hidden="true" /> Updated</dt>
                  <dd>{{ formatDisplayDateTime(photo.updatedAt) }}</dd>
                </template>
              </dl>

              <p v-if="copyMessage" class="small text-success mb-0" role="status">{{ copyMessage }}</p>
            </div>
          </div>
        </div>
      </div>

      <div class="card border-0 shadow-sm">
        <div class="card-body p-4">
          <div class="d-flex flex-wrap justify-content-between align-items-center mb-2">
            <h2 class="h5 mb-2 mb-sm-0">Location on the map</h2>
            <router-link :to="`/MapRenderer?photo=${photo.id}`" class="small font-weight-bold">
              Explore the surrounding area <span aria-hidden="true">→</span>
            </router-link>
          </div>
          <LocationPicker
            v-if="validCoordinates"
            :latitude="photo.latitude"
            :longitude="photo.longitude"
            :default-latitude="settings.map.defaultLatitude"
            :default-longitude="settings.map.defaultLongitude"
            :default-zoom="settings.map.defaultZoom"
            :min-zoom="settings.map.minimumZoom"
            :max-zoom="settings.map.maximumZoom"
            :layers="settings.map.layers"
            height="280px"
            readonly
            aria-label="Map showing the photograph location"
          />
          <p v-else class="small text-muted mb-0">This photograph does not have valid coordinates.</p>
          <p class="small text-muted mt-2 mb-0">
            Map data © OpenStreetMap contributors. Location shown in EPSG:4326 coordinates.
          </p>
        </div>
      </div>

      <ConfirmDialog
        v-if="showDeleteConfirm"
        title="Delete this photograph?"
        :message="`“${photo.title}” and its stored image will be permanently removed. This cannot be undone.`"
        confirm-label="Delete photograph"
        busy-label="Deleting…"
        :busy="deleting"
        @cancel="showDeleteConfirm = false"
        @confirm="deletePhoto"
      />

      <p v-if="deleteError" class="alert alert-danger mt-3 mb-0" role="alert">{{ deleteError }}</p>
    </template>
  </div>
</template>

<script>
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import LocationPicker from "@/components/LocationPicker.vue";
import { api, resolveMediaUrl } from "@/utils/api";
import { DEFAULT_SETTINGS, loadSettings } from "@/utils/settings";
import {
  formatCoordinates,
  formatDisplayDate,
  formatDisplayDateTime,
  isValidCoordinatePair,
  openStreetMapUrl,
} from "@/utils/geo";

export default {
  name: "PhotoDetail",
  components: { ConfirmDialog, LocationPicker },
  data() {
    return {
      settings: DEFAULT_SETTINGS,
      photo: null,
      loading: true,
      imageFailed: false,
      copyMessage: "",
      showDeleteConfirm: false,
      deleting: false,
      deleteError: "",
    };
  },
  computed: {
    validCoordinates() {
      return this.photo && isValidCoordinatePair(this.photo.latitude, this.photo.longitude);
    },
    osmUrl() {
      if (!this.photo || !this.validCoordinates) return "";
      return openStreetMapUrl(this.photo.latitude, this.photo.longitude);
    },
  },
  async mounted() {
    this.settings = await loadSettings();
    await this.loadPhoto();
  },
  methods: {
    resolveMediaUrl,
    formatCoordinates,
    formatDisplayDate,
    formatDisplayDateTime,

    async loadPhoto() {
      this.loading = true;
      try {
        this.photo = await api.get(`/api/photos/${this.$route.params.id}`);
      } catch {
        this.photo = null;
      } finally {
        this.loading = false;
      }
    },

    openImage() {
      if (!this.photo || !this.photo.imageUrl) return;
      window.open(resolveMediaUrl(this.photo.imageUrl), "_blank", "noopener");
    },

    async copyCoordinates() {
      if (!this.photo) return;
      const text = formatCoordinates(this.photo.latitude, this.photo.longitude);
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
        this.copyMessage = "Coordinates copied to the clipboard.";
      } catch {
        this.copyMessage = "Could not copy the coordinates.";
      }
    },

    async deletePhoto() {
      if (this.deleting) return;
      this.deleting = true;
      this.deleteError = "";
      try {
        await api.delete(`/api/photos/${this.photo.id}`);
        this.showDeleteConfirm = false;
        this.$router.push("/MyPhotos");
      } catch (error) {
        this.deleteError =
          error.status === 403
            ? "You do not have permission to delete this photograph."
            : error.message || "The photograph could not be deleted.";
      } finally {
        this.deleting = false;
      }
    },
  },
};
</script>

<style scoped>
.photo-detail {
  max-width: 1180px;
}

.empty-state {
  border-radius: 14px;
}

.detail-media {
  background: #0f172a;
  min-height: 320px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.detail-media__button {
  border: 0;
  padding: 0;
  background: transparent;
  width: 100%;
  height: 100%;
  cursor: zoom-in;
}

.detail-image {
  width: 100%;
  height: 100%;
  max-height: 620px;
  object-fit: contain;
  display: block;
}

.detail-placeholder {
  min-height: 320px;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #94a3b8;
}

.detail-title {
  font-size: clamp(1.4rem, 3vw, 2rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  color: #0f172a;
}

.detail-description {
  font-size: 1rem;
  line-height: 1.6;
  white-space: pre-line;
}

.detail-meta {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.35rem;
  margin-bottom: 0;
}

.detail-meta dt {
  color: #64748b;
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 700;
  margin-top: 0.7rem;
}

.detail-meta dd {
  margin: 0;
  color: #1e293b;
  font-size: 0.92rem;
}

.coordinate-text {
  font-variant-numeric: tabular-nums;
  font-family: Consolas, "Courier New", monospace;
  font-size: 0.85rem;
  color: #334155;
}
</style>
