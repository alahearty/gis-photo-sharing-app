<template>
  <div class="policy-page container py-3 py-lg-4">
    <div class="row justify-content-center">
      <div class="col-lg-9">
        <p class="text-uppercase small font-weight-bold text-primary mb-1">Application policy</p>
        <h1 class="h2 mb-2">Privacy policy</h1>
        <p class="text-muted small mb-4">Last updated: 23 October 2026</p>

        <div class="alert alert-warning" role="note">
          <strong>Policy status.</strong> This page describes how the GIS PhotoShare application
          currently handles data. It is not a substitute for legal advice. The operator of this
          deployment should review and approve the policy, and add an appropriate contact route,
          before using the application for a public service.
        </div>

        <h2 class="h4 mt-4">Who operates this service</h2>
        <p class="text-muted">
          GIS PhotoShare is an application for sharing geotagged photographs. This deployment is
          operated by the application owner. No personal contact details, company registration number
          or physical address is published in this demonstration; those details should be supplied by
          the operator where required by law.
        </p>

        <h2 class="h4 mt-4">Information the application collects</h2>
        <ul class="text-muted">
          <li><strong>Account information:</strong> your email address and a securely hashed password. Passwords are never stored in plain text.</li>
          <li><strong>Photographs:</strong> the image files you upload and their generated thumbnails.</li>
          <li><strong>Photograph metadata:</strong> title, description, category, capture date, upload date, location name, latitude, longitude and visibility setting.</li>
          <li><strong>Technical information:</strong> authentication tokens stored in your browser and ordinary server logs needed to operate and secure the service.</li>
          <li><strong>Optional GPS metadata:</strong> if you choose to apply location metadata found in an image, the resulting coordinates are saved with the photograph.</li>
        </ul>

        <h2 class="h4 mt-4">How information is used</h2>
        <p class="text-muted">
          The information is used to authenticate you, display your photographs, place them on the
          map, enforce ownership, provide search and filtering, and generate optional GeoJSON exports
          of public photographs. The application does not include advertising or analytics tracking.
        </p>

        <h2 class="h4 mt-4">Public and private photographs</h2>
        <p class="text-muted">
          Public photographs and their metadata can be viewed by anyone, including through the map,
          the photo API and GeoJSON exports. Private photographs are excluded from public maps,
          searches, feeds and exports and are available only to the owner. If you later make a private
          photograph public, it becomes visible to others. If you make a public photograph private, it
          is removed from public feeds.
        </p>

        <h2 class="h4 mt-4">Location data and third-party map services</h2>
        <p class="text-muted">
          Coordinates are stored in the application database. To display the map, your browser
          requests map tiles from the providers configured by the operator, currently OpenStreetMap,
          OpenTopoMap and Esri World Imagery. These providers may receive technical information such
          as your IP address as part of serving those tiles, subject to their own policies. If the
          optional reverse-geocoding feature is used, the selected coordinates are sent to the
          configured Nominatim service to look up an approximate place name.
        </p>

        <h2 class="h4 mt-4">Cookies and browser storage</h2>
        <p class="text-muted">
          The application does not use advertising cookies. It stores a JSON Web Token and basic
          account information in your browser’s local storage so that you remain signed in. Signing out
          removes this stored authentication data from the browser.
        </p>

        <h2 class="h4 mt-4">Retention and deletion</h2>
        <p class="text-muted">
          Photograph records and files remain until the owner deletes them. Deleting a photograph
          removes its database record and stored image files. Account deletion is not currently a
          self-service feature; the operator must provide a process for account deletion requests
          before production use.
        </p>

        <h2 class="h4 mt-4">Security</h2>
        <p class="text-muted">
          Passwords are hashed by ASP.NET Core Identity. Authenticated actions use short-lived bearer
          tokens and ownership checks are enforced on the server. Browser geolocation requires a
          secure context (HTTPS or localhost). No system is completely secure, and the operator should
          apply the deployment guidance in the project documentation.
        </p>

        <h2 class="h4 mt-4">Your choices</h2>
        <ul class="text-muted">
          <li>Change a photograph between public and private visibility.</li>
          <li>Edit or delete photographs you own.</li>
          <li>Sign out to remove the stored token from the browser.</li>
          <li>Decline browser location permission; map selection and manual coordinates remain available.</li>
        </ul>

        <h2 class="h4 mt-4">Changes to this policy</h2>
        <p class="text-muted mb-4">
          The policy may be updated as the application changes. The “last updated” date on this page
          should be revised when material changes are made.
        </p>

        <div class="d-flex flex-wrap">
          <router-link to="/Terms" class="btn btn-outline-primary mr-2 mb-2">Terms of use</router-link>
          <router-link to="/About" class="btn btn-link mb-2">About the application</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "Privacy",
};
</script>

<style scoped>
.policy-page {
  max-width: 1080px;
}

.policy-page h1,
.policy-page h2 {
  color: #0f172a;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.policy-page h2 {
  font-size: 1.15rem;
}

.policy-page p,
.policy-page li {
  line-height: 1.65;
}
</style>
