<template>
  <div id="app" class="app-shell">
    <a class="skip-link" href="#main-content">Skip to main content</a>
    <NavBarSection />
    <main id="main-content" class="app-main" tabindex="-1">
      <router-view />
    </main>
    <FooterSection />
  </div>
</template>

<script>
import FooterSection from "@/components/Footer.vue";
import NavBarSection from "@/components/NavBar.vue";

export default {
  name: "App",
  components: {
    FooterSection,
    NavBarSection,
  },
};
</script>

<style lang="scss">
html {
  scroll-behavior: smooth;
}

body {
  background: #f1f5f9;
}

.app-shell {
  min-height: 100vh;
  min-height: 100dvh;
  display: flex;
  flex-direction: column;
  font-family: "Segoe UI", system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #1e293b;
  background: linear-gradient(180deg, #f1f5f9 0%, #e2e8f0 100%);
}

/* Fills space between nav and footer; lets auth pages center vertically */
.app-main {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
  width: 100%;
  padding: 0.75rem 0 1.5rem;
}

.app-main:focus {
  outline: none;
}

/* Login / Sign up — centered in remaining viewport, not clipped under the browser chrome */
.auth-page {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem 1rem 2rem;
  width: 100%;
  box-sizing: border-box;
}

.skip-link {
  position: absolute;
  left: 0.75rem;
  top: -60px;
  z-index: 3000;
  background: #0f172a;
  color: #fff;
  padding: 0.55rem 0.9rem;
  border-radius: 0 0 8px 8px;
  font-weight: 600;
  transition: top 0.15s ease;
}

.skip-link:focus {
  top: 0;
  color: #fff;
  text-decoration: none;
}

a:focus-visible,
button:focus-visible,
input:focus-visible,
select:focus-visible,
textarea:focus-visible,
[tabindex]:focus-visible {
  outline: 3px solid rgba(14, 165, 233, 0.65);
  outline-offset: 2px;
}

button,
.btn,
.nav-link,
.custom-control-label {
  touch-action: manipulation;
}

@media (prefers-reduced-motion: reduce) {
  html {
    scroll-behavior: auto;
  }

  *,
  *::before,
  *::after {
    animation-duration: 0.001ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.001ms !important;
  }
}
</style>
