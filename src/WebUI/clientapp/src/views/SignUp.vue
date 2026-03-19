<template>
  <div class="container" style="max-width: 400px; margin-top: 3rem;">
    <div class="card">
      <div class="card-header">Sign Up</div>
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
              minlength="6"
              placeholder="At least 6 characters"
            />
          </div>
          <p v-if="error" class="text-danger small">{{ error }}</p>
          <button type="submit" class="btn btn-primary btn-block" :disabled="loading">
            {{ loading ? "Creating account..." : "Sign Up" }}
          </button>
        </form>
        <p class="mt-3 mb-0 small text-center">
          Already have an account? <router-link to="/Login">Login</router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: "SignUp",
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
        await this.$store.dispatch("register", {
          email: this.form.email,
          password: this.form.password,
        });
        this.$router.push("/");
      } catch (e) {
        this.error = e.message || "Registration failed.";
      }
      this.loading = false;
    },
  },
};
</script>
