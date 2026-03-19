<template>
  <div class="photo-upload container">
    <h2>Share a Photo</h2>
    <form @submit.prevent="submitPhoto" class="card p-4">
      <div class="form-group">
        <label for="file">Photo *</label>
        <input
          id="file"
          ref="fileInput"
          type="file"
          accept="image/jpeg,image/png,image/gif,image/webp"
          class="form-control-file"
          @change="onFileSelected"
        />
        <small class="form-text text-muted">JPEG, PNG, GIF, or WebP (max 10 MB)</small>
        <div v-if="previewUrl" class="mt-2">
          <img :src="previewUrl" alt="Preview" class="img-thumbnail" style="max-height: 200px;" />
        </div>
      </div>
      <div class="form-group">
        <label for="title">Title *</label>
        <input
          id="title"
          v-model="form.title"
          type="text"
          class="form-control"
          required
          maxlength="200"
        />
      </div>
      <div class="form-group">
        <label for="description">Description</label>
        <textarea
          id="description"
          v-model="form.description"
          class="form-control"
          rows="3"
        />
      </div>
      <div class="form-row">
        <div class="form-group col-md-6">
          <label for="lat">Latitude *</label>
          <input
            id="lat"
            v-model.number="form.latitude"
            type="number"
            step="0.000001"
            class="form-control"
            required
          />
        </div>
        <div class="form-group col-md-6">
          <label for="lng">Longitude *</label>
          <input
            id="lng"
            v-model.number="form.longitude"
            type="number"
            step="0.000001"
            class="form-control"
            required
          />
        </div>
      </div>
      <div class="mb-2">
        <button type="button" class="btn btn-outline-secondary btn-sm" @click="useCurrentLocation" :disabled="gettingLocation">
          {{ gettingLocation ? "Getting..." : "Use my location" }}
        </button>
      </div>
      <p class="text-muted small">
        Tip: Use the Map page to find coordinates, or click "Use my location".
      </p>
      <button type="submit" class="btn btn-primary" :disabled="saving">
        {{ saving ? "Uploading..." : "Share Photo" }}
      </button>
      <p v-if="message" :class="messageClass">{{ message }}</p>
    </form>
  </div>
</template>

<script>
export default {
  name: "PhotoUpload",
  data() {
    return {
      form: {
        title: "",
        description: "",
        latitude: 40.7128,
        longitude: -74.006,
      },
      selectedFile: null,
      previewUrl: null,
      saving: false,
      gettingLocation: false,
      message: "",
      messageClass: "",
    };
  },
  mounted() {
    this.useCurrentLocation();
  },
  methods: {
    onFileSelected(e) {
      const f = e.target.files?.[0];
      this.selectedFile = f || null;
      this.previewUrl = null;
      if (f) {
        this.previewUrl = URL.createObjectURL(f);
        if (!this.form.title) this.form.title = f.name.replace(/\.[^.]+$/, "");
      }
    },
    useCurrentLocation() {
      if (!navigator.geolocation) return;
      this.gettingLocation = true;
      navigator.geolocation.getCurrentPosition(
        (pos) => {
          this.form.latitude = pos.coords.latitude;
          this.form.longitude = pos.coords.longitude;
          this.gettingLocation = false;
        },
        () => { this.gettingLocation = false; }
      );
    },
    async submitPhoto() {
      if (!this.selectedFile) {
        this.message = "Please select a photo to upload.";
        this.messageClass = "text-danger";
        return;
      }
      this.saving = true;
      this.message = "";
      const baseUrl = process.env.VUE_APP_API_URL || "";
      try {
        const fd = new FormData();
        fd.append("title", this.form.title);
        fd.append("description", this.form.description || "");
        fd.append("latitude", String(this.form.latitude));
        fd.append("longitude", String(this.form.longitude));
        fd.append("file", this.selectedFile);

        const res = await fetch(`${baseUrl}/api/upload/photo`, {
          method: "POST",
          body: fd,
        });
        if (res.ok) {
          this.message = "Photo shared successfully! View it on the Map.";
          this.messageClass = "text-success";
          this.form.title = "";
          this.form.description = "";
          this.selectedFile = null;
          this.previewUrl = null;
          if (this.$refs.fileInput) this.$refs.fileInput.value = "";
          setTimeout(() => this.$router.push("/MapRenderer"), 1000);
        } else {
          const err = await res.json().catch(() => ({}));
          this.message = err.error || err.errors?.join?.() || "Failed to share photo. Please try again.";
          this.messageClass = "text-danger";
        }
      } catch (e) {
        this.message = "Network error. Is the API running at " + (baseUrl || "same origin") + "?";
        this.messageClass = "text-danger";
      }
      this.saving = false;
    },
  },
};
</script>

<style scoped>
.photo-upload {
  max-width: 600px;
  margin: 2rem auto;
}
</style>
