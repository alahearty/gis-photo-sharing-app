<template>
  <nav class="navbar navbar-expand-md navbar-dark app-navbar" aria-label="Primary navigation">
    <div class="container">
      <router-link to="/" class="navbar-brand d-flex align-items-center py-2" @click.native="closeMenu">
        <span class="brand-mark" aria-hidden="true">◉</span>
        <span class="brand-text">GIS PhotoShare</span>
      </router-link>

      <button
        class="navbar-toggler"
        type="button"
        aria-controls="navbarMain"
        :aria-expanded="menuOpen ? 'true' : 'false'"
        aria-label="Toggle navigation"
        @click="menuOpen = !menuOpen"
      >
        <span class="navbar-toggler-icon" />
      </button>

      <div ref="navbarCollapse" :class="['collapse', 'navbar-collapse', { show: menuOpen }]" id="navbarMain">
        <ul class="navbar-nav mr-auto">
          <li class="nav-item">
            <router-link to="/" class="nav-link" exact-active-class="active" @click.native="closeMenu">Home</router-link>
          </li>
          <li class="nav-item">
            <router-link to="/MapRenderer" class="nav-link" active-class="active" @click.native="closeMenu">Explore map</router-link>
          </li>
          <li v-if="$store.getters.isLoggedIn" class="nav-item">
            <router-link to="/MyPhotos" class="nav-link" active-class="active" @click.native="closeMenu">My photos</router-link>
          </li>
          <li class="nav-item">
            <router-link to="/PhotoUpload" class="nav-link" active-class="active" @click.native="closeMenu">Share photo</router-link>
          </li>
          <li class="nav-item">
            <router-link to="/About" class="nav-link" active-class="active" @click.native="closeMenu">About</router-link>
          </li>
        </ul>

        <ul class="navbar-nav align-items-md-center">
          <template v-if="$store.getters.isLoggedIn">
            <li ref="profileMenu" class="nav-item profile-menu">
              <button
                ref="profileButton"
                type="button"
                class="nav-link profile-button"
                :aria-expanded="profileOpen ? 'true' : 'false'"
                aria-haspopup="true"
                aria-controls="profile-menu"
                @click="toggleProfile"
              >
                <span class="profile-avatar" aria-hidden="true">{{ userInitial }}</span>
                <span class="d-none d-lg-inline ml-1">{{ $store.getters.displayName }}</span>
                <i class="fa fa-chevron-down ml-1 small" aria-hidden="true" />
              </button>
              <div v-if="profileOpen" id="profile-menu" class="profile-dropdown shadow" role="menu">
                <div class="profile-dropdown__head">
                  <strong class="d-block small">Signed in</strong>
                  <span class="text-muted small text-break">{{ userEmail }}</span>
                </div>
                <router-link to="/MyPhotos" class="profile-dropdown__item" role="menuitem" @click.native="closeProfile">
                  <i class="fa fa-th mr-2" aria-hidden="true" /> My photos
                </router-link>
                <router-link to="/PhotoUpload" class="profile-dropdown__item" role="menuitem" @click.native="closeProfile">
                  <i class="fa fa-camera mr-2" aria-hidden="true" /> Share a photo
                </router-link>
                <router-link to="/About" class="profile-dropdown__item" role="menuitem" @click.native="closeProfile">
                  <i class="fa fa-info-circle mr-2" aria-hidden="true" /> About
                </router-link>
                <button type="button" class="profile-dropdown__item profile-dropdown__item--danger" role="menuitem" @click="logout">
                  <i class="fa fa-sign-out mr-2" aria-hidden="true" /> Sign out
                </button>
              </div>
            </li>
          </template>
          <template v-else>
            <li class="nav-item">
              <router-link to="/SignUp" class="nav-link" active-class="active" @click.native="closeMenu">Sign up</router-link>
            </li>
            <li class="nav-item">
              <router-link to="/Login" class="nav-link btn-nav-login" active-class="active" @click.native="closeMenu">Log in</router-link>
            </li>
          </template>
        </ul>
      </div>
    </div>
  </nav>
</template>

<script>
export default {
  name: "NavBar",
  data() {
    return {
      profileOpen: false,
      menuOpen: false,
    };
  },
  computed: {
    userEmail() {
      return this.$store.state.user?.email || "Signed-in account";
    },
    userInitial() {
      const name = this.$store.getters.displayName || "A";
      return name.charAt(0).toUpperCase();
    },
  },
  mounted() {
    document.addEventListener("click", this.onDocumentClick);
    document.addEventListener("keydown", this.onKeyDown);
  },
  beforeDestroy() {
    document.removeEventListener("click", this.onDocumentClick);
    document.removeEventListener("keydown", this.onKeyDown);
  },
  methods: {
    toggleProfile() {
      this.profileOpen = !this.profileOpen;
    },
    closeProfile() {
      this.profileOpen = false;
    },
    onDocumentClick(event) {
      if (!this.profileOpen) return;
      const menu = this.$refs.profileMenu;
      if (menu && !menu.contains(event.target)) this.closeProfile();
    },
    onKeyDown(event) {
      if (event.key === "Escape") this.closeProfile();
    },
    closeMenu() {
      this.closeProfile();
      this.menuOpen = false;
    },
    logout() {
      this.$store.dispatch("logout");
      this.closeMenu();
      this.$router.push("/");
    },
  },
};
</script>

<style lang="scss" scoped>
.app-navbar {
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
  box-shadow: 0 2px 12px rgba(15, 23, 42, 0.35);
  z-index: 1030;
}

.brand-mark {
  font-size: 1.35rem;
  color: #38bdf8;
  margin-right: 0.5rem;
  line-height: 1;
}

.brand-text {
  font-weight: 700;
  letter-spacing: -0.02em;
  font-size: 1.1rem;
  color: #f8fafc;
}

.navbar-brand:hover .brand-text {
  color: #fff;
}

.nav-link {
  font-weight: 500;
  padding: 0.5rem 0.85rem !important;
  border-radius: 6px;
  transition: background 0.15s ease, color 0.15s ease;
}

.nav-link.active {
  color: #fff !important;
  background: rgba(56, 189, 248, 0.2);
}

.btn-nav-login {
  background: rgba(56, 189, 248, 0.25);
  margin-left: 0.25rem;
}

.btn-nav-login.active,
.btn-nav-login:hover {
  background: #0ea5e9 !important;
  color: #fff !important;
}

.profile-menu {
  position: relative;
}

.profile-button {
  display: flex;
  align-items: center;
  border: 0;
  background: transparent;
  color: #e2e8f0;
  cursor: pointer;
}

.profile-button:hover,
.profile-button:focus {
  color: #fff;
  outline: none;
  background: rgba(56, 189, 248, 0.16);
}

.profile-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  background: linear-gradient(145deg, #0ea5e9, #1d4ed8);
  color: #fff;
  font-weight: 700;
  font-size: 0.8rem;
}

.profile-dropdown {
  position: absolute;
  right: 0;
  top: calc(100% + 0.4rem);
  min-width: 230px;
  background: #fff;
  border-radius: 10px;
  padding: 0.35rem;
  z-index: 1200;
}

.profile-dropdown__head {
  padding: 0.55rem 0.65rem;
  border-bottom: 1px solid #e2e8f0;
  margin-bottom: 0.25rem;
}

.profile-dropdown__item {
  display: block;
  width: 100%;
  text-align: left;
  border: 0;
  background: transparent;
  color: #334155;
  padding: 0.5rem 0.65rem;
  border-radius: 6px;
  font-size: 0.9rem;
}

.profile-dropdown__item:hover,
.profile-dropdown__item:focus {
  background: #f1f5f9;
  color: #0f172a;
  text-decoration: none;
  outline: none;
}

.profile-dropdown__item--danger {
  color: #b91c1c;
}

.profile-dropdown__item--danger:hover {
  background: #fef2f2;
}

@media (max-width: 767.98px) {
  .profile-menu {
    width: 100%;
  }

  .profile-dropdown {
    position: static;
    box-shadow: none !important;
    border: 1px solid rgba(148, 163, 184, 0.25);
    margin: 0.35rem 0;
  }
}
</style>
