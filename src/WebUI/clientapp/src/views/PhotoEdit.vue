<template>
  <div class="photo-edit container py-3 py-lg-4">
    <div class="d-flex flex-wrap justify-content-between align-items-end mb-3">
      <div>
        <p class="text-uppercase small font-weight-bold text-primary mb-1">Manage photograph</p>
        <h1 class="h2 font-weight-bold mb-0">Edit photograph</h1>
      </div>
      <router-link v-if="photo" :to="`/photos/${photo.id}`" class="btn btn-outline-secondary btn-sm">
        <i class="fa fa-arrow-left mr-1" aria-hidden="true" /> Back to details
      </router-link>
    </div>

    <div v-if="loading" class="text-center py-5 text-muted">
      <span class="spinner-border spinner-border-sm mr-2" aria-hidden="true" /> Loading photograph…
    </div>

    <div v-else-if="!photo" class="alert alert-warning">
      This photograph could not be found, or you do not have access to it.
      <router-link to="/MyPhotos" class="alert-link">Return to My photos</router-link>.
    </div>

    <div v-else-if="!photo.isOwner" class="alert alert-warning">
      You can only edit photographs that you own.
      <router-link :to="`/photos/${photo.id}`" class="alert-link">View the photograph</router-link>.
    </div>

    <form v-else class="row" novalidate @submit.prevent="save">
      <div class="col-lg-7">
        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">Photograph details</h2>

            <div class="form-group">
              <label for="edit-title">Title <span class="text-danger">*</span></label>
              <input id="edit-title" v-model="form.title" type="text" class="form-control" maxlength="200" required />
              <small class="form-text text-muted">{{ form.title.length }}/200 characters</small>
            </div>

            <div class="form-group">
              <label for="edit-description">Description</label>
              <textarea id="edit-description" v-model="form.description" class="form-control" rows="4" maxlength="2000" />
              <small class="form-text text-muted">{{ form.description.length }}/2000 characters</small>
            </div>

            <div class="form-row">
              <div class="form-group col-sm-6">
                <label for="edit-category">Category</label>
                <select id="edit-category" v-model="form.category" class="form-control">
                  <option v-for="category in settings.photo.categories" :key="category" :value="category">
                    {{ category }}
                  </option>
                </select>
              </div>
              <div class="form-group col-sm-6">
                <label for="edit-date">Capture date</label>
                <input id="edit-date" v-model="form.takenAt" type="date" class="form-control" />
              </div>
            </div>

            <div class="form-group mb-0">
              <label for="edit-location-name">Location name</label>
              <div class="input-group">
                <input id="edit-location-name" v-model="form.locationName" type="text" class="form-control" maxlength="200" />
                <div class="input-group-append">
                  <button
                    type="button"
                    class="btn btn-outline-secondary"
                    :disabled="!hasCoordinates || geocodeLoading || !settings.geocoding.enabled"
                    @click="lookUpPlaceName"
                  >
                    <span v-if="geocodeLoading" class="spinner-border spinner-border-sm mr-1" aria-hidden="true" />
                    <i v-else class="fa fa-search mr-1" aria-hidden="true" />
                    Find place
                  </button>
                </div>
              </div>
              <small v-if="settings.geocoding.enabled" class="form-text text-muted">
                {{ settings.geocoding.attribution }}
              </small>
              <p v-if="geocodeMessage" class="small mt-1 mb-0" :class="geocodeError ? 'text-danger' : 'text-success'">
                {{ geocodeMessage }}
              </p>
            </div>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">Location</h2>

            <div class="location-methods mb-3">
              <button type="button" class="btn btn-sm btn-outline-primary" :disabled="geolocationBusy" @click="useMyLocation">
                <span v-if="geolocationBusy" class="spinner-border spinner-border-sm mr-1" aria-hidden="true" />
                <i v-else class="fa fa-crosshairs mr-1" aria-hidden="true" />
                {{ geolocationBusy ? "Finding your location…" : "Use my location" }}
              </button>
              <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="!hasCoordinates" @click="copyCoordinates">
                <i class="fa fa-copy mr-1" aria-hidden="true" /> Copy coordinates
              </button>
              <a
                v-if="hasCoordinates"
                :href="osmUrl"
                class="btn btn-sm btn-outline-secondary"
                target="_blank"
                rel="noopener noreferrer"
              >
                <i class="fa fa-external-link mr-1" aria-hidden="true" /> Open in OpenStreetMap
              </a>
            </div>

            <p v-if="geolocationMessage" class="small mb-3" :class="geolocationError ? 'text-danger' : 'text-success'">
              {{ geolocationMessage }}
            </p>

            <div class="form-row">
              <div class="form-group col-sm-6">
                <label for="edit-latitude">Latitude <span class="text-danger">*</span></label>
                <input
                  id="edit-latitude"
                  v-model="form.latitude"
                  type="text"
                  inputmode="decimal"
                  class="form-control"
                  :class="{ 'is-invalid': latitudeError }"
                  required
                  @input="onCoordinateInput"
                  @blur="normaliseCoordinate('latitude')"
                />
                <div v-if="latitudeError" class="invalid-feedback">{{ latitudeError }}</div>
              </div>
              <div class="form-group col-sm-6">
                <label for="edit-longitude">Longitude <span class="text-danger">*</span></label>
                <input
                  id="edit-longitude"
                  v-model="form.longitude"
                  type="text"
                  inputmode="decimal"
                  class="form-control"
                  :class="{ 'is-invalid': longitudeError }"
                  required
                  @input="onCoordinateInput"
                  @blur="normaliseCoordinate('longitude')"
                />
                <div v-if="longitudeError" class="invalid-feedback">{{ longitudeError }}</div>
              </div>
            </div>

            <LocationPicker
              ref="locationPicker"
              :latitude="latitudeNumber"
              :longitude="longitudeNumber"
              :default-latitude="settings.map.defaultLatitude"
              :default-longitude="settings.map.defaultLongitude"
              :default-zoom="settings.map.defaultZoom"
              :min-zoom="settings.map.minimumZoom"
              :max-zoom="settings.map.maximumZoom"
              :layers="settings.map.layers"
              height="300px"
              aria-label="Adjust the photograph location on a map"
              @selected="onMapLocationSelected"
            />
            <p v-if="coordinateError" class="text-danger small mt-2 mb-0" role="alert">{{ coordinateError }}</p>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">Visibility</h2>
            <fieldset>
              <legend class="sr-only">Photograph visibility</legend>
              <div class="custom-control custom-radio mb-2">
                <input id="edit-public" v-model="form.isPublic" name="edit-visibility" type="radio" class="custom-control-input" :value="true" />
                <label class="custom-control-label" for="edit-public">
                  <strong>Public</strong>
                  <span class="d-block small text-muted">Visible on the public map, searches and GeoJSON exports.</span>
                </label>
              </div>
              <div class="custom-control custom-radio">
                <input id="edit-private" v-model="form.isPublic" name="edit-visibility" type="radio" class="custom-control-input" :value="false" />
                <label class="custom-control-label" for="edit-private">
                  <strong>Only me</strong>
                  <span class="d-block small text-muted">Hidden from public maps, searches and exports.</span>
                </label>
              </div>
            </fieldset>
          </div>
        </div>

        <div v-if="message" class="alert" :class="messageClass" role="alert">{{ message }}</div>

        <div class="d-flex flex-wrap align-items-center">
          <button type="submit" class="btn btn-primary btn-lg px-4 mr-2" :disabled="saving">
            <span v-if="saving" class="spinner-border spinner-border-sm mr-2" aria-hidden="true" />
            {{ saving ? "Saving…" : "Save changes" }}
          </button>
          <router-link :to="`/photos/${photo.id}`" class="btn btn-link">Cancel</router-link>
        </div>
      </div>

      <div class="col-lg-5">
        <div class="edit-side">
          <div class="card border-0 shadow-sm mb-4">
            <div class="card-body p-4">
              <h2 class="h6 text-uppercase text-muted mb-3">Original photograph</h2>
              <img
                v-if="photo.imageUrl"
                :src="resolveMediaUrl(photo.imageUrl)"
                :alt="photo.title"
                class="edit-image"
              />
              <div v-else class="edit-image-placeholder"><i class="fa fa-picture-o" aria-hidden="true" /></div>
              <dl class="edit-meta mt-3 mb-0">
                <dt>Uploaded</dt>
                <dd>{{ formatDisplayDateTime(photo.createdAt) }}</dd>
                <dt>Coordinates</dt>
                <dd>{{ formatCoordinates(latitudeNumber, longitudeNumber) || "Not set" }}</dd>
                <dt>Current visibility</dt>
                <dd>{{ photo.isPublic ? "Public" : "Only me" }}</dd>
              </dl>
            </div>
          </div>
          <div class="alert alert-light border small mb-0">
            <i class="fa fa-shield text-primary mr-1" aria-hidden="true" />
            Only you can edit or delete this photograph.
          </div>
        </div>
      </div>
    </form>
  </div>
</template>

<script>
import LocationPicker from "@/components/LocationPicker.vue";
import { api, resolveMediaUrl } from "@/utils/api";
import { DEFAULT_SETTINGS, loadSettings } from "@/utils/settings";
import {
  formatCoordinate,
  formatCoordinates,
  formatDisplayDateTime,
  isValidCoordinatePair,
  isValidLatitude,
  isValidLongitude,
  openStreetMapUrl,
  toNumber,
} from "@/utils/geo";

export default {
  name: "PhotoEdit",
  components: { LocationPicker },
  data() {
    return {
      settings: DEFAULT_SETTINGS,
      photo: null,
      loading: true,
      saving: false,
      form: {
        title: "",
        description: "",
        latitude: "",
        longitude: "",
        locationName: "",
        category: "General",
        takenAt: "",
        isPublic: true,
      },
      latitudeError: "",
      longitudeError: "",
      message: "",
      messageClass: "alert-danger",
      geolocationBusy: false,
      geolocationMessage: "",
      geolocationError: false,
      geocodeLoading: false,
      geocodeMessage: "",
      geocodeError: false,
    };
  },
  computed: {
    latitudeNumber() {
      return toNumber(this.form.latitude);
    },
    longitudeNumber() {
      return toNumber(this.form.longitude);
    },
    hasCoordinates() {
      return isValidCoordinatePair(this.latitudeNumber, this.longitudeNumber);
    },
    coordinateError() {
      if (!this.form.latitude && !this.form.longitude) return "";
      if (!isValidLatitude(this.form.latitude) || !isValidLongitude(this.form.longitude)) {
        return "Enter a valid latitude (-90 to 90) and longitude (-180 to 180).";
      }
      return "";
    },
    osmUrl() {
      return this.hasCoordinates ? openStreetMapUrl(this.latitudeNumber, this.longitudeNumber) : "";
    },
  },
  async mounted() {
    this.settings = await loadSettings();
    await this.loadPhoto();
  },
  methods: {
    resolveMediaUrl,
    formatCoordinates,
    formatDisplayDateTime,

    async loadPhoto() {
      this.loading = true;
      try {
        const photo = await api.get(`/api/photos/${this.$route.params.id}`);
        this.photo = photo;
        this.form = {
          title: photo.title || "",
          description: photo.description || "",
          latitude: formatCoordinate(photo.latitude),
          longitude: formatCoordinate(photo.longitude),
          locationName: photo.locationName || "",
          category: photo.category || this.settings.photo.categories[0] || "General",
          takenAt: photo.takenAt || "",
          isPublic: photo.isPublic !== false,
        };
      } catch {
        this.photo = null;
      } finally {
        this.loading = false;
      }
    },

    onMapLocationSelected({ latitude, longitude }) {
      this.setCoordinates(latitude, longitude, false);
    },

    setCoordinates(latitude, longitude, pan) {
      this.form.latitude = formatCoordinate(latitude);
      this.form.longitude = formatCoordinate(longitude);
      this.latitudeError = "";
      this.longitudeError = "";
      if (pan && this.$refs.locationPicker) {
        this.$refs.locationPicker.panTo(latitude, longitude);
      }
    },

    onCoordinateInput() {
      this.latitudeError = "";
      this.longitudeError = "";
    },

    normaliseCoordinate(field) {
      const value = this.form[field];
      if (field === "latitude") {
        if (value === "") {
          this.latitudeError = "";
          return;
        }
        this.latitudeError = isValidLatitude(value) ? "" : "Latitude must be between -90 and 90.";
        if (!this.latitudeError) this.form.latitude = formatCoordinate(value);
      } else {
        if (value === "") {
          this.longitudeError = "";
          return;
        }
        this.longitudeError = isValidLongitude(value) ? "" : "Longitude must be between -180 and 180.";
        if (!this.longitudeError) this.form.longitude = formatCoordinate(value);
      }

      if (this.hasCoordinates && this.$refs.locationPicker) {
        this.$refs.locationPicker.panTo(this.latitudeNumber, this.longitudeNumber);
      }
    },

    useMyLocation() {
      this.geolocationMessage = "";
      this.geolocationError = false;

      if (!navigator.geolocation) {
        this.geolocationError = true;
        this.geolocationMessage = "Geolocation is not supported by this browser. Pick a point on the map instead.";
        return;
      }

      this.geolocationBusy = true;
      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.geolocationBusy = false;
          const { latitude, longitude, accuracy } = position.coords;
          this.setCoordinates(latitude, longitude, true);
          this.geolocationError = false;
          this.geolocationMessage = accuracy
            ? `Location found, accurate to about ${Math.round(accuracy)} metres.`
            : "Location found.";
        },
        (error) => {
          this.geolocationBusy = false;
          this.geolocationError = true;
          if (error.code === 1) {
            this.geolocationMessage = "Location permission was denied. Pick a point on the map instead.";
          } else if (error.code === 2) {
            this.geolocationMessage = "Your location is currently unavailable. Pick a point on the map instead.";
          } else {
            this.geolocationMessage = "Finding your location timed out. Try again or pick a point on the map.";
          }
        },
        { enableHighAccuracy: true, timeout: 12000, maximumAge: 0 }
      );
    },

    async lookUpPlaceName() {
      if (!this.hasCoordinates || this.geocodeLoading) return;
      this.geocodeLoading = true;
      this.geocodeMessage = "";
      this.geocodeError = false;

      try {
        const result = await api.get("/api/geocoding/reverse", {
          latitude: this.latitudeNumber,
          longitude: this.longitudeNumber,
        });
        const name = result.placeName || result.displayName;
        if (name) {
          this.form.locationName = name;
          this.geocodeMessage = "Place name added. You can edit it.";
        } else {
          this.geocodeError = true;
          this.geocodeMessage = "No place name was found near these coordinates.";
        }
      } catch (error) {
        this.geocodeError = true;
        this.geocodeMessage = error.message || "The geocoding service is unavailable.";
      } finally {
        this.geocodeLoading = false;
      }
    },

    async copyCoordinates() {
      if (!this.hasCoordinates) return;
      const text = formatCoordinates(this.latitudeNumber, this.longitudeNumber);
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
        this.geolocationError = false;
        this.geolocationMessage = "Coordinates copied to the clipboard.";
      } catch {
        this.geolocationError = true;
        this.geolocationMessage = "Could not copy the coordinates.";
      }
    },

    async save() {
      if (this.saving) return;
      this.message = "";

      if (!this.form.title.trim()) {
        this.message = "Enter a title for the photograph.";
        this.messageClass = "alert-danger";
        return;
      }
      if (!this.hasCoordinates) {
        this.message = "Set a valid latitude and longitude.";
        this.messageClass = "alert-danger";
        return;
      }

      this.saving = true;
      try {
        await api.put(`/api/photos/${this.photo.id}`, {
          title: this.form.title.trim(),
          description: this.form.description.trim(),
          latitude: Number(formatCoordinate(this.latitudeNumber)),
          longitude: Number(formatCoordinate(this.longitudeNumber)),
          locationName: this.form.locationName.trim(),
          category: this.form.category || "General",
          takenAt: this.form.takenAt || "",
          isPublic: this.form.isPublic,
        });
        this.$router.push(`/photos/${this.photo.id}`);
      } catch (error) {
        if (error.status === 403) {
          this.message = "You do not have permission to edit this photograph.";
        } else if (error.status === 401) {
          this.message = "Your session has expired. Sign in again.";
        } else {
          this.message = error.message || "The changes could not be saved.";
        }
        this.messageClass = "alert-danger";
      } finally {
        this.saving = false;
      }
    },
  },
};
</script>

<style scoped>
.photo-edit {
  max-width: 1180px;
}

.edit-side {
  position: sticky;
  top: 1rem;
}

.edit-image {
  width: 100%;
  max-height: 300px;
  object-fit: cover;
  border-radius: 10px;
}

.edit-image-placeholder {
  height: 200px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f1f5f9;
  color: #94a3b8;
  border-radius: 10px;
  font-size: 2rem;
}

.edit-meta {
  display: grid;
  grid-template-columns: 110px 1fr;
  gap: 0.4rem 0.75rem;
  font-size: 0.85rem;
}

.edit-meta dt {
  color: #64748b;
  font-weight: 600;
}

.edit-meta dd {
  margin: 0;
  color: #1e293b;
}

.location-methods {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

@media (max-width: 991.98px) {
  .edit-side {
    position: static;
  }
}
</style>
