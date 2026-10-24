<template>
  <div class="upload-page container py-3 py-lg-4">
    <div class="upload-heading mb-4">
      <p class="text-uppercase small font-weight-bold text-primary mb-1">Contribute to the map</p>
      <h1 class="h2 font-weight-bold mb-2">Share a photograph</h1>
      <p class="text-muted mb-0">
        Add an image, choose where it was taken, and publish it to the Port Harcourt photo map.
      </p>
    </div>

    <div v-if="success" class="success-panel card border-0 shadow-sm">
      <div class="card-body p-4 p-lg-5">
        <div class="row align-items-center">
          <div class="col-md-4 mb-3 mb-md-0">
            <img v-if="success.imageUrl" :src="success.imageUrl" :alt="success.title" class="success-image" />
            <div v-else class="success-placeholder"><i class="fa fa-check" aria-hidden="true" /></div>
          </div>
          <div class="col-md-8">
            <p class="text-success font-weight-bold mb-1"><i class="fa fa-check-circle mr-1" aria-hidden="true" /> Photo shared</p>
            <h2 class="h4">{{ success.title }}</h2>
            <p class="text-muted">Your photograph has been saved and is now available on the map.</p>
            <div class="d-flex flex-wrap">
              <router-link :to="`/MapRenderer?photo=${success.id}`" class="btn btn-primary mr-2 mb-2">
                <i class="fa fa-map-marker mr-1" aria-hidden="true" /> View on map
              </router-link>
              <router-link to="/MyPhotos" class="btn btn-outline-primary mr-2 mb-2">
                <i class="fa fa-th mr-1" aria-hidden="true" /> My photos
              </router-link>
              <router-link :to="`/photos/${success.id}`" class="btn btn-outline-secondary mr-2 mb-2">
                View details
              </router-link>
              <button type="button" class="btn btn-link mb-2" @click="uploadAnother">Upload another</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <form v-else class="row" novalidate @submit.prevent="submitPhoto">
      <div class="col-lg-7">
        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">1. Choose a photograph</h2>

            <div
              class="dropzone"
              :class="{ 'dropzone--active': dragging, 'dropzone--error': !!fileError }"
              role="button"
              tabindex="0"
              :aria-label="selectedFile ? 'Replace selected photograph' : 'Choose a photograph'"
              @click="openFilePicker"
              @keydown.enter.prevent="openFilePicker"
              @keydown.space.prevent="openFilePicker"
              @dragover.prevent="dragging = true"
              @dragleave.prevent="dragging = false"
              @drop.prevent="onDrop"
            >
              <template v-if="previewUrl">
                <img :src="previewUrl" alt="Selected photograph preview" class="dropzone__preview" />
                <div class="dropzone__overlay">
                  <span><i class="fa fa-refresh mr-1" aria-hidden="true" /> Choose a different image</span>
                </div>
              </template>
              <template v-else>
                <i class="fa fa-cloud-upload fa-2x text-primary mb-2" aria-hidden="true" />
                <p class="font-weight-bold mb-1">Drag and drop a photograph here</p>
                <p class="text-muted small mb-2">or click to browse</p>
                <p class="text-muted small mb-0">
                  {{ allowedTypesLabel }} · maximum {{ maxSizeLabel }}
                </p>
              </template>
            </div>
            <input
              ref="fileInput"
              type="file"
              class="sr-only"
              :accept="acceptAttribute"
              aria-label="Photograph file"
              @change="onFileInput"
            />
            <div class="d-flex flex-wrap align-items-center mt-2">
              <button v-if="selectedFile" type="button" class="btn btn-sm btn-outline-secondary mr-2" @click="clearFile">
                <i class="fa fa-times mr-1" aria-hidden="true" /> Remove selection
              </button>
              <span v-if="selectedFile" class="small text-muted">
                {{ selectedFile.name }} · {{ fileSizeLabel(selectedFile.size) }}
              </span>
            </div>
            <p v-if="fileError" class="text-danger small mt-2 mb-0" role="alert">{{ fileError }}</p>

            <div v-if="exifStatus === 'checking'" class="exif-note mt-3" role="status">
              <span class="spinner-border spinner-border-sm text-primary mr-2" aria-hidden="true" />
              Checking the image for GPS information…
            </div>
            <div v-else-if="exifStatus === 'found'" class="exif-note exif-note--found mt-3">
              <div class="d-flex justify-content-between align-items-start">
                <div>
                  <strong class="small"><i class="fa fa-location-arrow mr-1" aria-hidden="true" /> GPS location detected</strong>
                  <p class="small mb-1">{{ formatCoordinates(exif.latitude, exif.longitude) }}</p>
                  <p v-if="exif.takenAt" class="small text-muted mb-0">
                    Captured {{ formatDisplayDate(exif.takenAt) }}
                  </p>
                </div>
                <button type="button" class="btn btn-sm btn-outline-primary ml-2" @click="applyExifLocation">
                  Use detected location
                </button>
              </div>
            </div>
            <div v-else-if="exifStatus === 'none'" class="exif-note mt-3">
              <i class="fa fa-info-circle mr-1" aria-hidden="true" />
              No GPS location was found in this image. Use the map or enter coordinates manually.
            </div>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">2. Describe the photograph</h2>

            <div class="form-group">
              <label for="photo-title">Title <span class="text-danger">*</span></label>
              <input
                id="photo-title"
                v-model="form.title"
                type="text"
                class="form-control"
                maxlength="200"
                required
                placeholder="For example, Rumuola flyover at dusk"
              />
              <small class="form-text text-muted">{{ form.title.length }}/200 characters</small>
            </div>

            <div class="form-group">
              <label for="photo-description">Description</label>
              <textarea
                id="photo-description"
                v-model="form.description"
                class="form-control"
                rows="3"
                maxlength="2000"
                placeholder="What is happening in this photograph?"
              />
              <small class="form-text text-muted">{{ form.description.length }}/2000 characters</small>
            </div>

            <div class="form-row">
              <div class="form-group col-sm-6">
                <label for="photo-category">Category</label>
                <select id="photo-category" v-model="form.category" class="form-control">
                  <option v-for="category in settings.photo.categories" :key="category" :value="category">
                    {{ category }}
                  </option>
                </select>
              </div>
              <div class="form-group col-sm-6">
                <label for="photo-date">Capture date</label>
                <input id="photo-date" v-model="form.takenAt" type="date" class="form-control" />
              </div>
            </div>

            <div class="form-group mb-0">
              <label for="photo-location-name">Location name</label>
              <div class="input-group">
                <input
                  id="photo-location-name"
                  v-model="form.locationName"
                  type="text"
                  class="form-control"
                  maxlength="200"
                  placeholder="For example, Port Harcourt city centre"
                />
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
              <p v-if="geocodeMessage" class="small mt-1 mb-0" :class="geocodeError ? 'text-danger' : 'text-success'" role="status">
                {{ geocodeMessage }}
              </p>
            </div>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">3. Set the location</h2>

            <div class="location-methods mb-3">
              <button type="button" class="btn btn-outline-primary btn-sm" :disabled="geolocationBusy" @click="useMyLocation">
                <span v-if="geolocationBusy" class="spinner-border spinner-border-sm mr-1" aria-hidden="true" />
                <i v-else class="fa fa-crosshairs mr-1" aria-hidden="true" />
                {{ geolocationBusy ? "Finding your location…" : "Use my location" }}
              </button>
              <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="!hasCoordinates" @click="copyCoordinates">
                <i class="fa fa-copy mr-1" aria-hidden="true" /> Copy coordinates
              </button>
              <a
                v-if="hasCoordinates"
                :href="osmUrl"
                class="btn btn-outline-secondary btn-sm"
                target="_blank"
                rel="noopener noreferrer"
              >
                <i class="fa fa-external-link mr-1" aria-hidden="true" /> Open in OpenStreetMap
              </a>
            </div>

            <p v-if="geolocationMessage" class="small mb-3" :class="geolocationError ? 'text-danger' : 'text-success'" role="status">
              {{ geolocationMessage }}
              <span v-if="geolocationNeedsHttps" class="d-block text-muted">Browser location requires HTTPS or localhost.</span>
            </p>

            <div class="form-row">
              <div class="form-group col-sm-6">
                <label for="photo-latitude">Latitude <span class="text-danger">*</span></label>
                <input
                  id="photo-latitude"
                  v-model="form.latitude"
                  type="text"
                  inputmode="decimal"
                  class="form-control"
                  :class="{ 'is-invalid': latitudeError }"
                  placeholder="4.815600"
                  autocomplete="off"
                  @input="onCoordinateInput"
                  @blur="normaliseCoordinate('latitude')"
                />
                <div v-if="latitudeError" class="invalid-feedback">{{ latitudeError }}</div>
                <small v-else class="form-text text-muted">Between -90 and 90</small>
              </div>
              <div class="form-group col-sm-6">
                <label for="photo-longitude">Longitude <span class="text-danger">*</span></label>
                <input
                  id="photo-longitude"
                  v-model="form.longitude"
                  type="text"
                  inputmode="decimal"
                  class="form-control"
                  :class="{ 'is-invalid': longitudeError }"
                  placeholder="7.049800"
                  autocomplete="off"
                  @input="onCoordinateInput"
                  @blur="normaliseCoordinate('longitude')"
                />
                <div v-if="longitudeError" class="invalid-feedback">{{ longitudeError }}</div>
                <small v-else class="form-text text-muted">Between -180 and 180</small>
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
              aria-label="Choose the photograph location on a map"
              @selected="onMapLocationSelected"
            />

            <div class="d-flex flex-wrap justify-content-between align-items-center mt-2">
              <p class="small text-muted mb-0">
                <i class="fa fa-info-circle mr-1" aria-hidden="true" />
                Click the map to place the pin, or drag it to adjust.
              </p>
              <button
                v-if="hasCoordinates"
                type="button"
                class="btn btn-link btn-sm p-0"
                @click="clearCoordinates"
              >
                Clear location
              </button>
            </div>
            <p v-if="coordinateError" class="text-danger small mt-2 mb-0" role="alert">{{ coordinateError }}</p>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body p-4">
            <h2 class="h5 mb-3">4. Choose who can see it</h2>
            <fieldset>
              <legend class="sr-only">Photo visibility</legend>
              <div class="custom-control custom-radio mb-2">
                <input
                  id="visibility-public"
                  v-model="form.isPublic"
                  name="visibility"
                  type="radio"
                  class="custom-control-input"
                  :value="true"
                />
                <label class="custom-control-label" for="visibility-public">
                  <strong>Public</strong>
                  <span class="d-block small text-muted">Anyone can see this photograph and its location on the map.</span>
                </label>
              </div>
              <div class="custom-control custom-radio">
                <input
                  id="visibility-private"
                  v-model="form.isPublic"
                  name="visibility"
                  type="radio"
                  class="custom-control-input"
                  :value="false"
                />
                <label class="custom-control-label" for="visibility-private">
                  <strong>Only me</strong>
                  <span class="d-block small text-muted">The photograph is hidden from public maps, exports and searches.</span>
                </label>
              </div>
            </fieldset>
          </div>
        </div>

        <div v-if="formError" class="alert alert-danger" role="alert">{{ formError }}</div>

        <div class="d-flex flex-wrap align-items-center mb-2">
          <button type="submit" class="btn btn-primary btn-lg px-4 mr-2" :disabled="saving">
            <span v-if="saving" class="spinner-border spinner-border-sm mr-2" aria-hidden="true" />
            <i v-else class="fa fa-upload mr-2" aria-hidden="true" />
            {{ saving ? "Uploading…" : "Share photo" }}
          </button>
          <button type="button" class="btn btn-link" :disabled="saving" @click="resetForm">Reset form</button>
        </div>
        <div v-if="saving" class="progress mt-2" style="height: 8px" role="progressbar" :aria-valuenow="uploadProgress" aria-valuemin="0" aria-valuemax="100">
          <div class="progress-bar progress-bar-striped progress-bar-animated" :style="{ width: `${uploadProgress}%` }" />
        </div>
        <p v-if="saving" class="small text-muted mt-1 mb-0">{{ uploadProgress }}% uploaded</p>
      </div>

      <div class="col-lg-5">
        <div class="upload-side">
          <div class="card border-0 shadow-sm mb-4">
            <div class="card-body p-4">
              <h2 class="h6 text-uppercase text-muted mb-3">Preview</h2>
              <div class="preview-frame">
                <img v-if="previewUrl" :src="previewUrl" alt="Photograph preview" class="preview-image" />
                <div v-else class="preview-empty">
                  <i class="fa fa-picture-o fa-2x text-muted mb-2" aria-hidden="true" />
                  <span class="small text-muted">Your selected photograph will appear here.</span>
                </div>
              </div>
              <dl class="preview-meta mt-3 mb-0">
                <dt>Location</dt>
                <dd>{{ hasCoordinates ? formatCoordinates(latitudeNumber, longitudeNumber) : "Not set yet" }}</dd>
                <dt>Category</dt>
                <dd>{{ form.category || "General" }}</dd>
                <dt>Visibility</dt>
                <dd>{{ form.isPublic ? "Public" : "Only me" }}</dd>
              </dl>
            </div>
          </div>

          <div class="card border-0 shadow-sm">
            <div class="card-body p-4">
              <h2 class="h6 text-uppercase text-muted mb-3">Tips for a good contribution</h2>
              <ul class="small text-muted pl-3 mb-0">
                <li class="mb-2">Use the original photograph where possible; GPS metadata may be included.</li>
                <li class="mb-2">Place the pin accurately — it controls where the photo appears on the map.</li>
                <li class="mb-2">Add a location name to make the photograph easier to find.</li>
                <li>Private photographs are visible only to you.</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </form>
  </div>
</template>

<script>
import exifr from "exifr";
import LocationPicker from "@/components/LocationPicker.vue";
import { api, getStoredToken, resolveMediaUrl } from "@/utils/api";
import { DEFAULT_SETTINGS, loadSettings } from "@/utils/settings";
import {
  formatCoordinate,
  formatCoordinates,
  formatDisplayDate,
  isValidCoordinatePair,
  isValidLatitude,
  isValidLongitude,
  openStreetMapUrl,
  toNumber,
} from "@/utils/geo";

function toDateInput(value) {
  if (!value) return "";
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export default {
  name: "PhotoUpload",
  components: { LocationPicker },
  data() {
    return {
      settings: DEFAULT_SETTINGS,
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
      selectedFile: null,
      previewUrl: null,
      dragging: false,
      fileError: "",
      formError: "",
      latitudeError: "",
      longitudeError: "",
      saving: false,
      uploadProgress: 0,
      success: null,
      successImageUrl: "",
      exifStatus: "idle",
      exif: null,
      geolocationBusy: false,
      geolocationMessage: "",
      geolocationError: false,
      geolocationNeedsHttps: false,
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
      if (!this.hasCoordinates) return "";
      return openStreetMapUrl(this.latitudeNumber, this.longitudeNumber);
    },
    maxSizeLabel() {
      const bytes = this.settings.photo.maxUploadBytes || DEFAULT_SETTINGS.photo.maxUploadBytes;
      const megabytes = bytes / (1024 * 1024);
      return `${megabytes % 1 === 0 ? megabytes : megabytes.toFixed(1)} MB`;
    },
    acceptAttribute() {
      return (this.settings.photo.allowedContentTypes || []).join(",");
    },
    allowedTypesLabel() {
      return "JPEG, PNG, GIF or WebP";
    },
  },
  async mounted() {
    this.settings = await loadSettings();
    this.form.category = this.settings.photo.categories[0] || "General";
    this.applyQueryCoordinates();
    window.addEventListener("beforeunload", this.handleBeforeUnload);
  },
  beforeDestroy() {
    window.removeEventListener("beforeunload", this.handleBeforeUnload);
    if (this._xhr) this._xhr.abort();
    this.revokeObjectUrl(this.previewUrl);
    this.revokeObjectUrl(this.successImageUrl);
  },
  methods: {
    formatCoordinates,
    formatDisplayDate,

    fileSizeLabel(bytes) {
      if (!bytes && bytes !== 0) return "";
      if (bytes < 1024) return `${bytes} B`;
      if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
      return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    },

    applyQueryCoordinates() {
      const { lat, lng } = this.$route.query;
      if (isValidCoordinatePair(lat, lng)) {
        this.form.latitude = formatCoordinate(lat);
        this.form.longitude = formatCoordinate(lng);
      }
    },

    openFilePicker() {
      if (this.saving) return;
      if (this.$refs.fileInput) this.$refs.fileInput.click();
    },

    onFileInput(event) {
      const file = event.target.files && event.target.files[0];
      if (file) this.setFile(file);
      event.target.value = "";
    },

    onDrop(event) {
      this.dragging = false;
      const file = event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files[0];
      if (file) this.setFile(file);
    },

    validateFile(file) {
      if (!file) return "Choose a photograph to upload.";
      const allowed = this.settings.photo.allowedContentTypes || DEFAULT_SETTINGS.photo.allowedContentTypes;
      if (!allowed.includes(file.type)) {
        return "Choose a JPEG, PNG, GIF or WebP image.";
      }
      const maxBytes = this.settings.photo.maxUploadBytes || DEFAULT_SETTINGS.photo.maxUploadBytes;
      if (file.size > maxBytes) {
        return `The image must not exceed ${this.maxSizeLabel}.`;
      }
      return "";
    },

    async setFile(file) {
      const error = this.validateFile(file);
      this.fileError = error;
      if (error) return;

      this.revokeObjectUrl(this.previewUrl);
      this.selectedFile = file;
      this.previewUrl = URL.createObjectURL(file);
      this.success = null;
      if (!this.form.title) {
        this.form.title = file.name.replace(/\.[^.]+$/, "").replace(/[-_]+/g, " ").trim();
      }

      await this.detectExif(file);
    },

    clearFile() {
      this.revokeObjectUrl(this.previewUrl);
      this.selectedFile = null;
      this.previewUrl = null;
      this.fileError = "";
      this.exifStatus = "idle";
      this.exif = null;
    },

    async detectExif(file) {
      this.exifStatus = "checking";
      this.exif = null;
      try {
        let gps = null;
        try {
          gps = await exifr.gps(file);
        } catch {
          gps = null;
        }

        let parsed = null;
        try {
          parsed = await exifr.parse(file, ["DateTimeOriginal", "CreateDate"]);
        } catch {
          parsed = null;
        }

        const takenAtRaw = parsed && (parsed.DateTimeOriginal || parsed.CreateDate);
        const takenAt = toDateInput(takenAtRaw);
        if (takenAt && !this.form.takenAt) this.form.takenAt = takenAt;

        if (gps && isValidCoordinatePair(gps.latitude, gps.longitude)) {
          this.exif = {
            latitude: gps.latitude,
            longitude: gps.longitude,
            takenAt,
          };
          this.exifStatus = "found";
        } else {
          this.exifStatus = "none";
        }
      } catch {
        this.exifStatus = "none";
      }
    },

    applyExifLocation() {
      if (!this.exif) return;
      this.setCoordinates(this.exif.latitude, this.exif.longitude, true);
      this.exifStatus = "none";
      this.geolocationMessage = "The GPS location from the photograph was applied. You can adjust it on the map.";
      this.geolocationError = false;
    },

    onMapLocationSelected({ latitude, longitude }) {
      this.setCoordinates(latitude, longitude, false);
      this.latitudeError = "";
      this.longitudeError = "";
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
      this.geolocationMessage = "";
    },

    normaliseCoordinate(field) {
      const value = this.form[field];
      if (value === "") {
        if (field === "latitude") this.latitudeError = "";
        else this.longitudeError = "";
        return;
      }

      if (field === "latitude") {
        this.latitudeError = isValidLatitude(value) ? "" : "Latitude must be between -90 and 90.";
        if (!this.latitudeError) this.form.latitude = formatCoordinate(value);
      } else {
        this.longitudeError = isValidLongitude(value) ? "" : "Longitude must be between -180 and 180.";
        if (!this.longitudeError) this.form.longitude = formatCoordinate(value);
      }

      if (this.hasCoordinates && this.$refs.locationPicker) {
        this.$refs.locationPicker.panTo(this.latitudeNumber, this.longitudeNumber);
      }
    },

    clearCoordinates() {
      this.form.latitude = "";
      this.form.longitude = "";
      this.latitudeError = "";
      this.longitudeError = "";
      if (this.$refs.locationPicker) {
        this.$refs.locationPicker.panTo(
          this.settings.map.defaultLatitude,
          this.settings.map.defaultLongitude,
          this.settings.map.defaultZoom
        );
      }
    },

    useMyLocation() {
      this.geolocationMessage = "";
      this.geolocationError = false;
      this.geolocationNeedsHttps = false;

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
            ? `Location found, accurate to about ${Math.round(accuracy)} metres. Adjust the pin if needed.`
            : "Location found. Adjust the pin if needed.";
        },
        (error) => {
          this.geolocationBusy = false;
          this.geolocationError = true;
          if (error.code === 1) {
            this.geolocationMessage = "Location permission was denied. Pick a point on the map instead.";
            this.geolocationNeedsHttps =
              window.location.protocol !== "https:" &&
              window.location.hostname !== "localhost" &&
              window.location.hostname !== "127.0.0.1";
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

    validateForm() {
      if (!this.selectedFile) return "Select a photograph to upload.";
      if (!this.form.title.trim()) return "Enter a title for the photograph.";
      if (this.form.title.trim().length > 200) return "The title must not exceed 200 characters.";
      if (this.form.description.length > 2000) return "The description must not exceed 2,000 characters.";
      if (!this.hasCoordinates) return "Set the latitude and longitude, either manually or on the map.";
      if (this.form.locationName.trim().length > 200) return "The location name must not exceed 200 characters.";
      return "";
    },

    submitPhoto() {
      if (this.saving) return;

      this.formError = this.validateForm();
      if (this.formError) return;

      this.saving = true;
      this.uploadProgress = 0;

      const formData = new FormData();
      formData.append("title", this.form.title.trim());
      formData.append("description", this.form.description.trim());
      formData.append("latitude", formatCoordinate(this.latitudeNumber));
      formData.append("longitude", formatCoordinate(this.longitudeNumber));
      formData.append("locationName", this.form.locationName.trim());
      formData.append("category", this.form.category || "General");
      formData.append("isPublic", this.form.isPublic ? "true" : "false");
      formData.append("takenAt", this.form.takenAt || "");
      formData.append("file", this.selectedFile);

      const baseUrl = process.env.VUE_APP_API_URL || "";
      const xhr = new XMLHttpRequest();
      this._xhr = xhr;
      xhr.open("POST", `${baseUrl}/api/upload/photo`);

      const token = getStoredToken();
      if (token) xhr.setRequestHeader("Authorization", `Bearer ${token}`);

      xhr.upload.onprogress = (event) => {
        if (event.lengthComputable) {
          this.uploadProgress = Math.round((event.loaded / event.total) * 100);
        }
      };

      xhr.onload = async () => {
        this.saving = false;
        this._xhr = null;
        let payload = null;
        try {
          payload = JSON.parse(xhr.responseText);
        } catch {
          payload = null;
        }

        if (xhr.status >= 200 && xhr.status < 300) {
          this.uploadProgress = 100;
          const id = payload && payload.id;
          let imageUrl = this.previewUrl;
          if (id) {
            try {
              const saved = await api.get(`/api/photos/${id}`);
              imageUrl = resolveMediaUrl(saved.thumbnailUrl || saved.imageUrl || imageUrl);
            } catch {
              imageUrl = resolveMediaUrl(imageUrl);
            }
          }

          this.successImageUrl = this.previewUrl;
          this.previewUrl = null;
          this.success = {
            id,
            title: this.form.title.trim(),
            imageUrl,
          };
          this.selectedFile = null;
          this._xhr = null;
        } else if (xhr.status === 401) {
          this.formError = "Your session has expired. Sign in again to share a photograph.";
        } else if (xhr.status === 413) {
          this.formError = `The image is too large. Choose an image below ${this.maxSizeLabel}.`;
        } else {
          this.formError =
            (payload && (payload.error || payload.detail || payload.title)) ||
            "The photograph could not be uploaded. Please try again.";
        }
      };

      xhr.onerror = () => {
        this.saving = false;
        this._xhr = null;
        this.formError = "Could not connect to the server. Check your connection and try again.";
      };

      xhr.onabort = () => {
        this.saving = false;
        this._xhr = null;
      };

      xhr.send(formData);
    },

    uploadAnother() {
      this.revokeObjectUrl(this.successImageUrl);
      this.successImageUrl = "";
      this.success = null;
      this.resetForm();
      window.scrollTo({ top: 0, behavior: "smooth" });
    },

    resetForm() {
      this.revokeObjectUrl(this.previewUrl);
      this.previewUrl = null;
      this.selectedFile = null;
      this.form = {
        title: "",
        description: "",
        latitude: "",
        longitude: "",
        locationName: "",
        category: this.settings.photo.categories[0] || "General",
        takenAt: "",
        isPublic: true,
      };
      this.fileError = "";
      this.formError = "";
      this.latitudeError = "";
      this.longitudeError = "";
      this.exifStatus = "idle";
      this.exif = null;
      this.geolocationMessage = "";
      this.geolocationError = false;
      this.geocodeMessage = "";
      this.geocodeError = false;
      this.uploadProgress = 0;
      if (this.$refs.locationPicker && this.settings.map) {
        this.$refs.locationPicker.panTo(
          this.settings.map.defaultLatitude,
          this.settings.map.defaultLongitude,
          this.settings.map.defaultZoom
        );
      }
    },

    revokeObjectUrl(url) {
      if (url && String(url).startsWith("blob:")) {
        URL.revokeObjectURL(url);
      }
    },

    handleBeforeUnload(event) {
      if (this.selectedFile && !this.success) {
        event.preventDefault();
        event.returnValue = "";
      }
    },
  },
};
</script>

<style scoped>
.upload-page {
  max-width: 1180px;
}

.upload-heading h1 {
  letter-spacing: -0.03em;
}

.dropzone {
  position: relative;
  border: 2px dashed #bae6fd;
  background: #f8fbff;
  border-radius: 12px;
  padding: 2rem 1rem;
  text-align: center;
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
  overflow: hidden;
  min-height: 210px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.dropzone:hover,
.dropzone:focus {
  border-color: #0ea5e9;
  background: #f0f9ff;
  outline: none;
}

.dropzone--active {
  border-color: #0284c7;
  background: #e0f2fe;
}

.dropzone--error {
  border-color: #fca5a5;
  background: #fff7f7;
}

.dropzone__preview {
  width: 100%;
  max-height: 300px;
  object-fit: contain;
  border-radius: 8px;
}

.dropzone__overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(15, 23, 42, 0.55);
  color: #fff;
  opacity: 0;
  transition: opacity 0.15s ease;
  font-weight: 600;
  font-size: 0.9rem;
}

.dropzone:hover .dropzone__overlay,
.dropzone:focus .dropzone__overlay {
  opacity: 1;
}

.exif-note {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 0.7rem 0.85rem;
  font-size: 0.85rem;
  color: #475569;
}

.exif-note--found {
  background: #ecfdf5;
  border-color: #a7f3d0;
  color: #065f46;
}

.location-methods {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.preview-frame {
  width: 100%;
  height: 230px;
  background: #f1f5f9;
  border-radius: 10px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}

.preview-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.preview-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 1rem;
}

.preview-meta {
  display: grid;
  grid-template-columns: 90px 1fr;
  gap: 0.35rem 0.75rem;
  font-size: 0.85rem;
}

.preview-meta dt {
  color: #64748b;
  font-weight: 600;
}

.preview-meta dd {
  color: #1e293b;
  margin: 0;
}

.upload-side {
  position: sticky;
  top: 1rem;
}

.success-panel {
  border-radius: 14px;
}

.success-image {
  width: 100%;
  max-height: 260px;
  object-fit: cover;
  border-radius: 10px;
}

.success-placeholder {
  height: 180px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #ecfdf5;
  color: #059669;
  font-size: 3rem;
  border-radius: 10px;
}

@media (max-width: 991.98px) {
  .upload-side {
    position: static;
  }
}
</style>
