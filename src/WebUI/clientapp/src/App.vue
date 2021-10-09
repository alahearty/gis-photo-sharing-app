<template>
  <div id="app" style="background-color:light-gray">
    <NavBarSection />

    <main class="container">
      <router-view />
    </main>

    <FooterSection />
  </div>
</template>

<script>
import FooterSection from "@/components/Footer.vue";
import NavBarSection from "@/components/NavBar.vue";

export default {
  name: "footerSection",
  components: {
    FooterSection,
    NavBarSection,
  },
  methods: {
    name() {},
  },
  computed: {
    sma() {
      return this.data;
    },
  },
  watch: {},
};
</script>

<style lang="scss">
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}
.container {
  min-height: 80vh;
}
</style>
