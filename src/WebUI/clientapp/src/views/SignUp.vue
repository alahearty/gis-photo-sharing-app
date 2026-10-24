<template>
  <div class="auth-page">
    <div class="auth-panel">
      <header class="auth-panel__head">
        <div class="auth-panel__icon" aria-hidden="true">◉</div>
        <h1 class="auth-panel__title">Create an account</h1>
        <p class="auth-panel__sub">
          Join with your email and a password of at least 8 characters, including a letter and a number.
        </p>
      </header>
      <div class="auth-panel__body">
        <form @submit.prevent="submit" novalidate>
          <div class="form-group">
            <label class="auth-label" for="signup-email">Email</label>
            <input
              id="signup-email"
              v-model="form.email"
              type="email"
              class="form-control auth-input"
              required
              placeholder="you@example.com"
              autocomplete="email"
            />
          </div>
          <div class="form-group">
            <label class="auth-label" for="signup-password">Password</label>
            <input
              id="signup-password"
              v-model="form.password"
              type="password"
              class="form-control auth-input"
              required
              minlength="8"
              placeholder="At least 8 characters, with a letter and number"
              autocomplete="new-password"
            />
          </div>
          <p v-if="error" class="auth-error" role="alert">{{ error }}</p>
          <button type="submit" class="btn auth-btn btn-block" :disabled="loading">
            {{ loading ? "Creating account…" : "Sign up" }}
          </button>
        </form>
        <p class="auth-switch">
          Already have an account?
          <router-link to="/Login">Log in</router-link>
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
      this.error = "";
      if (!this.form.email.trim()) {
        this.error = "Enter your email address.";
        return;
      }
      if (!this.form.password || this.form.password.length < 8) {
        this.error = "Password must be at least 8 characters.";
        return;
      }
      if (!/[A-Za-z]/.test(this.form.password) || !/[0-9]/.test(this.form.password)) {
        this.error = "Password must include at least one letter and one number.";
        return;
      }

      this.loading = true;
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

<style scoped>
.auth-panel {
  width: 100%;
  max-width: 420px;
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 12px 40px rgba(15, 23, 42, 0.1), 0 4px 12px rgba(15, 23, 42, 0.06);
  overflow: hidden;
  border: 1px solid rgba(148, 163, 184, 0.25);
}

.auth-panel__head {
  background: linear-gradient(145deg, #0f172a 0%, #1e3a5f 55%, #0c4a6e 100%);
  color: #f8fafc;
  padding: 1.75rem 1.5rem 1.5rem;
  text-align: center;
}

.auth-panel__icon {
  font-size: 2rem;
  line-height: 1;
  color: #38bdf8;
  margin-bottom: 0.5rem;
}

.auth-panel__title {
  font-size: 1.35rem;
  font-weight: 800;
  letter-spacing: -0.03em;
  margin: 0 0 0.35rem;
}

.auth-panel__sub {
  margin: 0;
  font-size: 0.9rem;
  color: #cbd5e1;
  line-height: 1.45;
}

.auth-panel__body {
  padding: 1.75rem 1.5rem 1.5rem;
}

.auth-label {
  display: block;
  font-size: 0.8rem;
  font-weight: 600;
  color: #475569;
  margin-bottom: 0.35rem;
}

.auth-input {
  border-radius: 10px;
  border: 1px solid #cbd5e1;
  padding: 0.65rem 0.85rem;
  font-size: 1rem;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.auth-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 3px rgba(14, 165, 233, 0.2);
}

.auth-error {
  color: #dc2626;
  font-size: 0.85rem;
  margin: -0.25rem 0 0.75rem;
}

.auth-btn {
  margin-top: 0.25rem;
  padding: 0.7rem 1rem;
  font-weight: 600;
  font-size: 1rem;
  border-radius: 10px;
  border: none;
  background: linear-gradient(180deg, #0ea5e9 0%, #0284c7 100%);
  color: #fff;
  box-shadow: 0 4px 14px rgba(14, 165, 233, 0.35);
}

.auth-btn:hover:not(:disabled) {
  background: linear-gradient(180deg, #38bdf8 0%, #0ea5e9 100%);
  color: #fff;
}

.auth-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.auth-switch {
  margin: 1.25rem 0 0;
  padding-top: 1.25rem;
  border-top: 1px solid #e2e8f0;
  text-align: center;
  font-size: 0.9rem;
  color: #64748b;
  margin-bottom: 0;
}

.auth-switch a {
  font-weight: 600;
  color: #0284c7;
}

.auth-switch a:hover {
  color: #0369a1;
  text-decoration: none;
}
</style>
