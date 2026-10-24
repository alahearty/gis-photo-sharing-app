<template>
  <div class="policy-page container py-3 py-lg-4">
    <div class="row justify-content-center">
      <div class="col-lg-9">
        <p class="text-uppercase small font-weight-bold text-primary mb-1">Application policy</p>
        <h1 class="h2 mb-2">Terms of use</h1>
        <p class="text-muted small mb-4">Last updated: 23 October 2026</p>

        <div class="alert alert-warning" role="note">
          <strong>Review status.</strong> These terms describe the current demonstration application.
          They are not legal advice and do not assert a specific governing jurisdiction. The operator
          of this deployment should obtain legal review and add its own identity and contact details
          before using the service publicly.
        </div>

        <h2 class="h4 mt-4">1. Using the service</h2>
        <p class="text-muted">
          By using GIS PhotoShare, you agree to use it lawfully and responsibly. You must not attempt
          to access accounts, photographs or systems without authorisation, interfere with the
          service, bypass upload or validation limits, or use the application to distribute unlawful,
          harmful or infringing material.
        </p>

        <h2 class="h4 mt-4">2. Accounts</h2>
        <p class="text-muted">
          You are responsible for keeping your password secure and for activity carried out through
          your account. Provide accurate registration information and do not share your account
          credentials. The operator may suspend access where necessary to protect the service or other
          users.
        </p>

        <h2 class="h4 mt-4">3. Your photographs</h2>
        <p class="text-muted">
          You retain responsibility for the photographs you upload. By publishing a photograph as
          public, you allow the application to display it, its metadata and its coordinates to other
          users and through the public API and GeoJSON export. You must have the right to share the
          image and must not upload content that infringes copyright, privacy or other rights.
        </p>
        <p class="text-muted">
          Private photographs are hidden from public feeds and exports, but remain associated with
          your account. Only you can edit or delete your photographs. Deletion is permanent for the
          stored image and its record.
        </p>

        <h2 class="h4 mt-4">4. Location accuracy</h2>
        <p class="text-muted">
          Coordinates supplied by device location, image metadata, map selection or manual entry may
          be inaccurate. The application displays the values provided and does not guarantee that a
          photograph’s mapped position reflects the real world. Do not rely on this service for
          navigation, emergency response, surveying or safety-critical decisions.
        </p>

        <h2 class="h4 mt-4">5. Acceptable content</h2>
        <p class="text-muted">
          Do not upload executable files, malicious content, images you do not have permission to
          share, or material that violates applicable law. Uploads are restricted to supported image
          formats and size limits, and the server validates image content before storage.
        </p>

        <h2 class="h4 mt-4">6. Availability and changes</h2>
        <p class="text-muted">
          The service is provided on an “as available” basis. Features, map layers, storage and access
          may change or be withdrawn. The operator should publish a maintenance and backup policy
          appropriate to the deployment.
        </p>

        <h2 class="h4 mt-4">7. Third-party services</h2>
        <p class="text-muted">
          Map tiles and optional reverse geocoding are supplied by third parties and are subject to
          their own terms and attribution requirements. The application does not control their
          availability or accuracy.
        </p>

        <h2 class="h4 mt-4">8. Liability</h2>
        <p class="text-muted">
          To the extent permitted by law, the operator is not responsible for loss arising from use of
          the application, inaccurate coordinates, unavailable map tiles, user-supplied content or
          loss of uploaded images. The operator should obtain jurisdiction-specific legal advice on
          limitations of liability and consumer rights.
        </p>

        <h2 class="h4 mt-4">9. Privacy</h2>
        <p class="text-muted">
          The handling of personal data is described in the
          <router-link to="/Privacy">privacy policy</router-link>.
        </p>

        <h2 class="h4 mt-4">10. Contact and review</h2>
        <p class="text-muted mb-4">
          No personal contact details or company registration information is published in this
          demonstration. Before production use, the operator must add a valid contact route, identify
          the responsible legal entity and confirm that these terms meet applicable requirements.
        </p>

        <div class="d-flex flex-wrap">
          <router-link to="/Privacy" class="btn btn-outline-primary mr-2 mb-2">Privacy policy</router-link>
          <router-link to="/About" class="btn btn-link mb-2">About the application</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "Terms",
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

.policy-page p {
  line-height: 1.65;
}
</style>
