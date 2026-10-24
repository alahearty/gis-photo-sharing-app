<template>
  <footer class="app-footer">
    <div class="container py-4 py-md-5">
      <div class="row">
        <div class="col-lg-5 mb-4 mb-lg-0">
          <div class="footer-brand mb-3">
            <span class="footer-mark" aria-hidden="true">◉</span>
            <span class="footer-title">GIS PhotoShare</span>
          </div>
          <p class="footer-tagline small mb-3">
            Explore and share geotagged photographs through interactive maps, with a focus on
            Port Harcourt, Rivers State, Nigeria.
          </p>
          <p class="footer-small mb-0">
            Map data © <a href="https://www.openstreetmap.org/copyright" rel="noopener noreferrer" target="_blank">OpenStreetMap</a>
            contributors. Additional map providers are credited in the map layer control.
          </p>
        </div>

        <div class="col-6 col-lg-3 mb-4 mb-lg-0">
          <h2 class="footer-heading">Explore</h2>
          <nav class="footer-links" aria-label="Explore links">
            <router-link to="/">Home</router-link>
            <router-link to="/MapRenderer">Explore map</router-link>
            <router-link to="/PhotoUpload">Share photo</router-link>
            <router-link to="/MyPhotos">My photos</router-link>
          </nav>
        </div>

        <div class="col-6 col-lg-4">
          <h2 class="footer-heading">Information</h2>
          <nav class="footer-links" aria-label="Information links">
            <router-link to="/About">About</router-link>
            <router-link to="/Privacy">Privacy policy</router-link>
            <router-link to="/Terms">Terms of use</router-link>
          </nav>
          <p class="footer-small mt-3 mb-0">
            Coordinates are stored as WGS 84 (EPSG:4326) latitude and longitude values and displayed on
            EPSG:3857 web maps.
          </p>
        </div>
      </div>

      <hr class="footer-rule mt-4 mb-3" />
      <div class="d-flex flex-column flex-sm-row justify-content-between align-items-center">
        <p class="footer-small mb-1 mb-sm-0">&copy; {{ year }} GIS PhotoShare. All rights reserved.</p>
        <p class="footer-small mb-0">Built with ASP.NET Core, Vue and Leaflet.</p>
      </div>
    </div>
  </footer>
</template>

<script>
export default {
  name: "FooterSection",
  computed: {
    year() {
      return new Date().getFullYear();
    },
  },
};
</script>

<style scoped>
.app-footer {
  flex-shrink: 0;
  background: #0f172a;
  color: #94a3b8;
  margin-top: auto;
}

.footer-mark {
  color: #38bdf8;
  font-size: 1.25rem;
  margin-right: 0.35rem;
}

.footer-title {
  color: #f1f5f9;
  font-weight: 700;
  font-size: 1.1rem;
}

.footer-tagline {
  line-height: 1.55;
  max-width: 340px;
}

.footer-small {
  font-size: 0.78rem;
  line-height: 1.5;
  color: #94a3b8;
}

.footer-small a {
  color: #cbd5e1;
}

.footer-small a:hover {
  color: #38bdf8;
}

.footer-heading {
  color: #e2e8f0;
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.07em;
  margin-bottom: 0.9rem;
}

.footer-links {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}

.footer-links a {
  color: #cbd5e1;
  font-size: 0.88rem;
}

.footer-links a:hover {
  color: #38bdf8;
  text-decoration: none;
}

.footer-rule {
  border-color: rgba(148, 163, 184, 0.2);
}
</style>
