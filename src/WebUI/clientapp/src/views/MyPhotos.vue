<template>
  <div class="my-photos container py-3 py-lg-4">
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-end mb-4">
      <div>
        <p class="text-uppercase small font-weight-bold text-primary mb-1">Your collection</p>
        <h1 class="h2 font-weight-bold mb-1">My photos</h1>
        <p class="text-muted mb-0">Search, manage and map the photographs you have shared.</p>
      </div>
      <router-link to="/PhotoUpload" class="btn btn-primary mt-3 mt-md-0">
        <i class="fa fa-plus mr-1" aria-hidden="true" /> Share a photo
      </router-link>
    </div>

    <div class="card border-0 shadow-sm mb-4">
      <div class="card-body p-3">
        <div class="gallery-filters">
          <div class="filter-field filter-field--search">
            <label for="gallery-search">Search</label>
            <div class="input-group input-group-sm">
              <div class="input-group-prepend">
                <span class="input-group-text"><i class="fa fa-search" aria-hidden="true" /></span>
              </div>
              <input
                id="gallery-search"
                v-model="search"
                type="search"
                class="form-control"
                placeholder="Title, description or place"
                @input="onSearchInput"
              />
            </div>
          </div>
          <div class="filter-field">
            <label for="gallery-category">Category</label>
            <select id="gallery-category" v-model="category" class="form-control form-control-sm" @change="applyFilters">
              <option value="">All categories</option>
              <option v-for="item in settings.photo.categories" :key="item" :value="item">{{ item }}</option>
            </select>
          </div>
          <div class="filter-field">
            <label for="gallery-sort">Sort by</label>
            <select id="gallery-sort" v-model="sort" class="form-control form-control-sm" @change="applyFilters">
              <option value="newest">Newest first</option>
              <option value="oldest">Oldest first</option>
              <option value="title">Title A–Z</option>
              <option value="title_desc">Title Z–A</option>
            </select>
          </div>
          <div class="filter-field filter-field--actions">
            <button type="button" class="btn btn-sm btn-outline-secondary" :disabled="!hasFilters" @click="clearFilters">
              <i class="fa fa-times mr-1" aria-hidden="true" /> Clear
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="error" class="alert alert-danger d-flex flex-wrap justify-content-between align-items-center" role="alert">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-dark mt-2 mt-sm-0" @click="loadPhotos">Try again</button>
    </div>

    <div v-if="loading" class="text-center py-5 text-muted">
      <span class="spinner-border spinner-border-sm mr-2" aria-hidden="true" /> Loading your photographs…
    </div>

    <template v-else-if="photos.length">
      <p class="small text-muted mb-3">
        {{ totalCount }} {{ totalCount === 1 ? "photograph" : "photographs" }}
        <span v-if="totalPages > 1">· page {{ page }} of {{ totalPages }}</span>
      </p>

      <div class="photo-grid" role="list">
        <article
          v-for="photo in photos"
          :key="photo.id"
          class="photo-card card border-0 shadow-sm"
          role="listitem"
        >
          <router-link :to="`/photos/${photo.id}`" class="photo-card__media" :aria-label="`View ${photo.title}`">
            <img
              v-if="photo.thumbnailUrl || photo.imageUrl"
              :src="resolveMediaUrl(photo.thumbnailUrl || photo.imageUrl)"
              :alt="photo.title"
              loading="lazy"
              decoding="async"
              @error="onImageError"
            />
            <div v-else class="photo-card__placeholder"><i class="fa fa-picture-o" aria-hidden="true" /></div>
            <span class="photo-card__badge" :class="photo.isPublic ? 'photo-card__badge--public' : 'photo-card__badge--private'">
              <i :class="['fa', photo.isPublic ? 'fa-globe' : 'fa-lock', 'mr-1']" aria-hidden="true" />
              {{ photo.isPublic ? "Public" : "Only me" }}
            </span>
          </router-link>

          <div class="photo-card__body">
            <h2 class="h6 mb-1">
              <router-link :to="`/photos/${photo.id}`" class="text-dark">{{ photo.title }}</router-link>
            </h2>
            <p v-if="photo.locationName" class="small text-muted mb-1 text-truncate">
              <i class="fa fa-map-marker mr-1" aria-hidden="true" />{{ photo.locationName }}
            </p>
            <p class="small text-muted mb-2">
              <span v-if="photo.category" class="badge badge-light border mr-1">{{ photo.category }}</span>
              {{ formatDisplayDate(photo.createdAt) }}
            </p>
            <div class="photo-card__actions">
              <router-link :to="`/MapRenderer?photo=${photo.id}`" class="btn btn-sm btn-outline-primary">
                <i class="fa fa-map-o mr-1" aria-hidden="true" /> Map
              </router-link>
              <router-link :to="`/photos/${photo.id}/edit`" class="btn btn-sm btn-outline-secondary">
                <i class="fa fa-pencil mr-1" aria-hidden="true" /> Edit
              </router-link>
              <button type="button" class="btn btn-sm btn-outline-danger" @click="requestDelete(photo)">
                <i class="fa fa-trash mr-1" aria-hidden="true" /> Delete
              </button>
            </div>
          </div>
        </article>
      </div>

      <nav v-if="totalPages > 1" class="d-flex justify-content-between align-items-center mt-4" aria-label="Photo pages">
        <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="page <= 1" @click="goToPage(page - 1)">
          <i class="fa fa-chevron-left mr-1" aria-hidden="true" /> Previous
        </button>
        <span class="small text-muted">Page {{ page }} of {{ totalPages }}</span>
        <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="page >= totalPages" @click="goToPage(page + 1)">
          Next <i class="fa fa-chevron-right ml-1" aria-hidden="true" />
        </button>
      </nav>
    </template>

    <div v-else class="empty-state card border-0 shadow-sm">
      <div class="card-body p-5 text-center">
        <i class="fa fa-camera-retro fa-2x text-muted mb-3" aria-hidden="true" />
        <h2 class="h4">{{ hasFilters ? "No photographs match these filters" : "You have not shared a photograph yet" }}</h2>
        <p class="text-muted">
          {{ hasFilters ? "Try a different search, category or sort order." : "Upload your first geotagged photograph to start your collection." }}
        </p>
        <button v-if="hasFilters" type="button" class="btn btn-outline-primary mr-2" @click="clearFilters">Clear filters</button>
        <router-link to="/PhotoUpload" class="btn btn-primary">Share a photo</router-link>
      </div>
    </div>

    <ConfirmDialog
      v-if="deleteTarget"
      title="Delete this photograph?"
      :message="`“${deleteTarget.title}” and its stored image will be permanently removed. This cannot be undone.`"
      confirm-label="Delete photograph"
      busy-label="Deleting…"
      :busy="deleteBusy"
      @cancel="deleteTarget = null"
      @confirm="confirmDelete"
    />

    <p v-if="deleteError" class="alert alert-danger mt-3 mb-0" role="alert">{{ deleteError }}</p>
  </div>
</template>

<script>
import ConfirmDialog from "@/components/ConfirmDialog.vue";
import { api, resolveMediaUrl } from "@/utils/api";
import { DEFAULT_SETTINGS, loadSettings } from "@/utils/settings";
import { formatDisplayDate } from "@/utils/geo";

export default {
  name: "MyPhotos",
  components: { ConfirmDialog },
  data() {
    return {
      settings: DEFAULT_SETTINGS,
      photos: [],
      totalCount: 0,
      totalPages: 1,
      page: 1,
      pageSize: 12,
      search: "",
      category: "",
      sort: "newest",
      loading: true,
      error: "",
      deleteTarget: null,
      deleteBusy: false,
      deleteError: "",
    };
  },
  computed: {
    hasFilters() {
      return !!(this.search.trim() || this.category);
    },
  },
  async mounted() {
    this.settings = await loadSettings();
    await this.loadPhotos();
  },
  beforeDestroy() {
    if (this._searchTimer) clearTimeout(this._searchTimer);
  },
  methods: {
    resolveMediaUrl,
    formatDisplayDate,

    onSearchInput() {
      if (this._searchTimer) clearTimeout(this._searchTimer);
      this._searchTimer = setTimeout(() => this.applyFilters(), 350);
    },

    applyFilters() {
      this.page = 1;
      this.loadPhotos();
    },

    clearFilters() {
      this.search = "";
      this.category = "";
      this.page = 1;
      this.loadPhotos();
    },

    goToPage(page) {
      if (page < 1 || page > this.totalPages) return;
      this.page = page;
      this.loadPhotos();
      window.scrollTo({ top: 0, behavior: "smooth" });
    },

    async loadPhotos() {
      this.loading = true;
      this.error = "";
      try {
        const result = await api.get("/api/photos/gallery", {
          mine: "true",
          search: this.search.trim() || null,
          category: this.category || null,
          sort: this.sort,
          page: this.page,
          pageSize: this.pageSize,
        });
        this.photos = (result.items || []).filter(Boolean);
        this.totalCount = result.totalCount || 0;
        this.totalPages = Math.max(1, result.totalPages || 1);
        if (this.page > this.totalPages) {
          this.page = this.totalPages;
          await this.loadPhotos();
          return;
        }
      } catch (error) {
        if (error.status === 401) {
          this.error = "Your session has expired. Sign in again to view your photographs.";
        } else {
          this.error = error.message || "Could not load your photographs.";
        }
      } finally {
        this.loading = false;
      }
    },

    requestDelete(photo) {
      this.deleteError = "";
      this.deleteTarget = photo;
    },

    async confirmDelete() {
      if (!this.deleteTarget || this.deleteBusy) return;
      this.deleteBusy = true;
      this.deleteError = "";
      try {
        await api.delete(`/api/photos/${this.deleteTarget.id}`);
        this.deleteTarget = null;
        await this.loadPhotos();
      } catch (error) {
        this.deleteError =
          error.status === 403
            ? "You do not have permission to delete this photograph."
            : error.message || "The photograph could not be deleted.";
      } finally {
        this.deleteBusy = false;
      }
    },

    onImageError(event) {
      event.target.style.display = "none";
    },
  },
};
</script>

<style scoped>
.my-photos {
  max-width: 1240px;
}

.gallery-filters {
  display: grid;
  grid-template-columns: minmax(200px, 2fr) minmax(140px, 1fr) minmax(140px, 1fr) auto;
  gap: 0.6rem;
  align-items: end;
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

.photo-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
}

.photo-card {
  border-radius: 12px;
  overflow: hidden;
  transition: transform 0.18s ease, box-shadow 0.18s ease;
}

.photo-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.13) !important;
}

.photo-card__media {
  position: relative;
  display: block;
  height: 180px;
  background: #e2e8f0;
  overflow: hidden;
}

.photo-card__media img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 0.3s ease;
}

.photo-card:hover .photo-card__media img {
  transform: scale(1.03);
}

.photo-card__placeholder {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #94a3b8;
  font-size: 1.8rem;
}

.photo-card__badge {
  position: absolute;
  top: 8px;
  right: 8px;
  font-size: 0.68rem;
  font-weight: 700;
  padding: 0.2rem 0.45rem;
  border-radius: 999px;
  color: #fff;
}

.photo-card__badge--public {
  background: rgba(5, 150, 105, 0.9);
}

.photo-card__badge--private {
  background: rgba(51, 65, 85, 0.9);
}

.photo-card__body {
  padding: 0.85rem;
}

.photo-card__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
}

.empty-state {
  border-radius: 14px;
}

@media (max-width: 767.98px) {
  .gallery-filters {
    grid-template-columns: 1fr 1fr;
  }

  .filter-field--search,
  .filter-field--actions {
    grid-column: 1 / -1;
  }
}
</style>
