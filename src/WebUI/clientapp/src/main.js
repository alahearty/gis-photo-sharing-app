import Vue from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'

// Older builds registered a Workbox/PWA service worker; unregister so fresh assets load (Leaflet, no Mapbox SW).
if (typeof window !== 'undefined' && 'serviceWorker' in navigator) {
  navigator.serviceWorker.getRegistrations().then((regs) => {
    regs.forEach((r) => r.unregister());
  });
}

import "bootstrap/dist/css/bootstrap.min.css";
import "font-awesome/css/font-awesome.min.css";
Vue.config.productionTip = false

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app')
