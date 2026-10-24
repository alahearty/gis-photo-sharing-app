<template>
  <div class="about-page container py-3 py-lg-4">
    <div class="row justify-content-center">
      <div class="col-lg-8">
        <p class="text-uppercase small font-weight-bold text-primary mb-1">About the platform</p>
        <h1 class="h2 mb-3">GIS PhotoShare</h1>
        <p class="lead text-muted">
          GIS PhotoShare combines photography with geographic context. Every photograph can carry a
          latitude, longitude and optional place name, so it appears in the right place on an
          interactive map rather than in a location-less gallery.
        </p>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body">
            <h2 class="h5">A Port Harcourt focus</h2>
            <p class="text-muted mb-0">
              The default map view is centred on Port Harcourt, Rivers State, Nigeria
              (approximately 4.8156° N, 7.0498° E) at zoom level 12. Photographs elsewhere are still
              supported: use <strong>Fit photos</strong> to frame all visible markers or search for a
              place by name. The map does not automatically jump to a global extent when a
              photograph was taken outside the city.
            </p>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body">
            <h2 class="h5">How contributions work</h2>
            <ol class="text-muted pl-3 mb-0">
              <li class="mb-2">Create an account and sign in.</li>
              <li class="mb-2">Choose a JPEG, PNG, GIF or WebP photograph and add a title and description.</li>
              <li class="mb-2">
                Set the location by using device geolocation, clicking the map, entering coordinates
                manually, or applying GPS metadata detected in the image.
              </li>
              <li class="mb-2">Choose a category, optional capture date and public or private visibility.</li>
              <li>Publish it, then explore or manage it from the map and My photos.</li>
            </ol>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body">
            <h2 class="h5">Maps, coordinates and data sources</h2>
            <p class="text-muted">
              Coordinates are stored as WGS 84 (EPSG:4326) latitude and longitude values. Leaflet
              renders them on EPSG:3857 web-map tiles. The default OpenStreetMap layer requires no API
              key; additional layers are credited in the map’s layer control. Reverse geocoding, when
              used, is provided by Nominatim and is optional.
            </p>
            <p class="text-muted mb-0">
              Public photographs are available through the map and a GeoJSON export. Private
              photographs are shown only to their owner. Only the owner can edit or delete a
              photograph, and this is enforced by the server.
            </p>
          </div>
        </div>

        <div class="card border-0 shadow-sm mb-4">
          <div class="card-body">
            <h2 class="h5">Built with</h2>
            <p class="text-muted mb-0">
              ASP.NET Core 8, Entity Framework Core, ASP.NET Core Identity with JWT authentication,
              Vue 2, Bootstrap 4, Leaflet, and SixLabors.ImageSharp for server-side image processing.
              The application follows a layered architecture with command and query handlers.
            </p>
          </div>
        </div>

        <div class="d-flex flex-wrap">
          <router-link to="/MapRenderer" class="btn btn-primary mr-2 mb-2">Explore the map</router-link>
          <router-link to="/PhotoUpload" class="btn btn-outline-primary mr-2 mb-2">Share a photo</router-link>
          <router-link to="/Privacy" class="btn btn-link mb-2">Privacy policy</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "About",
};
</script>

<style scoped>
.about-page h1 {
  font-weight: 800;
  color: #0f172a;
  letter-spacing: -0.02em;
}
</style>
