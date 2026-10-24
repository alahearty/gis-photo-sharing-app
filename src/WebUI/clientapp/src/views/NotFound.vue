<template>
  <div class="not-found container py-5">
    <div class="row justify-content-center">
      <div class="col-lg-7 text-center">
        <p class="display-4 font-weight-bold text-primary mb-2">404</p>
        <h1 class="h3 mb-3">Page not found</h1>
        <p class="text-muted mb-4">
          The page you requested does not exist, or it may have moved.
        </p>
        <router-link to="/" class="btn btn-primary mr-2">Go home</router-link>
        <router-link to="/MapRenderer" class="btn btn-outline-primary">Explore the map</router-link>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "NotFound",
};
</script>
