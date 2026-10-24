<template>
  <div class="home w-100">
    <section class="hero text-center text-md-left mb-4">
      <div class="container py-4 py-lg-5">
        <div class="row align-items-center">
          <div class="col-lg-7 mb-4 mb-lg-0">
            <p class="hero-eyebrow mb-2">Geotagged photography · Port Harcourt</p>
            <h1 class="hero-title mb-3">Share photographs on the map</h1>
            <p class="hero-lead mb-4">
              Upload field photographs with their real-world coordinates, explore Port Harcourt and
              beyond through an interactive map, and manage a personal gallery of the images you own.
            </p>
            <div class="d-flex flex-column flex-sm-row justify-content-center justify-content-md-start">
              <router-link to="/MapRenderer" class="btn btn-lg btn-primary px-4 mb-2 mb-sm-0 mr-sm-3 shadow-sm">
                <i class="fa fa-map-o mr-2" aria-hidden="true" /> Explore the map
              </router-link>
              <router-link
                v-if="!$store.getters.isLoggedIn"
                to="/SignUp"
                class="btn btn-lg btn-outline-light border-2 px-4"
              >
                <i class="fa fa-user-plus mr-2" aria-hidden="true" /> Create account
              </router-link>
              <router-link v-else to="/PhotoUpload" class="btn btn-lg btn-outline-light border-2 px-4">
                <i class="fa fa-camera mr-2" aria-hidden="true" /> Share a photo
              </router-link>
            </div>
          </div>
          <div class="col-lg-5">
            <div class="hero-card shadow-lg rounded-lg p-4 text-left">
              <h2 class="h6 text-uppercase text-muted mb-3">Quick start</h2>
              <ol class="pl-3 mb-0 small hero-steps">
                <li class="mb-2">Sign up or log in to contribute.</li>
                <li class="mb-2">Choose a photograph and add a title.</li>
                <li class="mb-2">Pick its location on the map, use GPS or enter coordinates.</li>
                <li>Find it on the <strong>Explore map</strong> and in <strong>My photos</strong>.</li>
              </ol>
              <router-link to="/About" class="btn btn-sm btn-outline-primary mt-3">How it works</router-link>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="features py-4 py-lg-5">
      <div class="container">
        <h2 class="text-center h4 mb-4 section-title">Built for geographic storytelling</h2>
        <div class="row">
          <div class="col-md-4 mb-4">
            <div class="card feature-card h-100 border-0 shadow-sm">
              <div class="card-body">
                <div class="feature-icon mb-3"><i class="fa fa-map-marker text-primary" aria-hidden="true" /></div>
                <h3 class="h5">Map-first discovery</h3>
                <p class="text-muted small mb-0">
                  Explore clustered photo markers, search by place or category, filter by date and export
                  public metadata as GeoJSON.
                </p>
              </div>
            </div>
          </div>
          <div class="col-md-4 mb-4">
            <div class="card feature-card h-100 border-0 shadow-sm">
              <div class="card-body">
                <div class="feature-icon mb-3"><i class="fa fa-camera text-primary" aria-hidden="true" /></div>
                <h3 class="h5">Accurate locations</h3>
                <p class="text-muted small mb-0">
                  Use device location, pick a point on the map, enter coordinates manually, or apply GPS
                  metadata detected in the image.
                </p>
              </div>
            </div>
          </div>
          <div class="col-md-4 mb-4">
            <div class="card feature-card h-100 border-0 shadow-sm">
              <div class="card-body">
                <div class="feature-icon mb-3"><i class="fa fa-lock text-primary" aria-hidden="true" /></div>
                <h3 class="h5">You stay in control</h3>
                <p class="text-muted small mb-0">
                  Only you can edit or delete your photographs. Choose public visibility or keep a
                  photograph private to your account.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="port-harcourt-section pb-5">
      <div class="container">
        <div class="ph-card rounded-lg p-4 p-lg-5">
          <div class="row align-items-center">
            <div class="col-lg-8 mb-3 mb-lg-0">
              <p class="text-uppercase small font-weight-bold text-info mb-1">Local focus, global reach</p>
              <h2 class="h4 text-white mb-2">Centred on Port Harcourt, Rivers State</h2>
              <p class="text-light mb-0">
                The map opens on Port Harcourt city centre, while photographs shared elsewhere remain
                discoverable through the layer, search and “Fit photos” controls.
              </p>
            </div>
            <div class="col-lg-4 text-lg-right">
              <router-link to="/MapRenderer" class="btn btn-light">
                <i class="fa fa-location-arrow mr-1" aria-hidden="true" /> Visit Port Harcourt map
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
export default {
  name: "Home",
};
</script>

<style scoped>
.hero {
  background: linear-gradient(135deg, #0f172a 0%, #1e3a5f 50%, #0c4a6e 100%);
  color: #f8fafc;
  border-radius: 12px;
  box-shadow: 0 8px 32px rgba(15, 23, 42, 0.25);
}

.hero-eyebrow {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.12em;
  color: #7dd3fc;
  font-weight: 600;
}

.hero-title {
  font-size: clamp(1.75rem, 4vw, 2.5rem);
  font-weight: 800;
  letter-spacing: -0.03em;
  line-height: 1.15;
}

.hero-lead {
  font-size: 1.05rem;
  line-height: 1.6;
  color: #cbd5e1;
  max-width: 36rem;
}

.hero-card {
  background: rgba(255, 255, 255, 0.98);
  color: #334155;
}

.hero-steps li {
  line-height: 1.5;
}

.section-title {
  color: #475569;
  font-weight: 700;
}

.feature-card {
  border-radius: 12px !important;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.12) !important;
}

.feature-icon {
  font-size: 1.75rem;
  line-height: 1;
}

.feature-card h3 {
  font-weight: 700;
  color: #1e293b;
}

.ph-card {
  background: linear-gradient(120deg, #0f172a 0%, #0c4a6e 100%);
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.2);
}
</style>
