<template>
  <div class="container" style="max-width: 400px; margin-top: 3rem;">
    <div class="card">
      <div class="card-header">Login</div>
      <div class="card-body">
        <form @submit.prevent="submit">
          <div class="form-group">
            <label for="email">Email</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              class="form-control"
              required
              placeholder="you@example.com"
            />
          </div>
          <div class="form-group">
            <label for="password">Password</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              class="form-control"
              required
            />
          </div>
          <p v-if="error" class="text-danger small">{{ error }}</p>
          <button type="submit" class="btn btn-primary btn-block" :disabled="loading">
            {{ loading ? "Logging in..." : "Login" }}
          </button>
        </form>
        <p class="mt-3 mb-0 small text-center">
          Don't have an account? <router-link to="/SignUp">Sign up</router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "Login",
  data() {
    return {
      form: { email: "", password: "" },
      loading: false,
      error: "",
    };
  },
  methods: {
    async submit() {
      this.loading = true;
      this.error = "";
      try {
        await this.$store.dispatch("login", {
          email: this.form.email,
          password: this.form.password,
        });
        this.$router.push("/");
      } catch (e) {
        this.error = e.message || "Login failed.";
      }
      this.loading = false;
    },
  },
};
</script>
