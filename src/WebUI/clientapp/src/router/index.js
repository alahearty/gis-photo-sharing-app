import Vue from 'vue'
import VueRouter from 'vue-router'
import Home from '../views/Home.vue'
import store from '@/store'

Vue.use(VueRouter)

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home,
    meta: { title: 'GIS PhotoShare' },
  },
  {
    path: '/about',
    alias: '/About',
    name: 'About',
    component: () => import(/* webpackChunkName: "about" */ '../views/About.vue'),
    meta: { title: 'About · GIS PhotoShare' },
  },
  {
    path: '/Login',
    alias: '/login',
    name: 'Login',
    component: () => import(/* webpackChunkName: "auth" */ '../views/Login.vue'),
    meta: { title: 'Log in · GIS PhotoShare' },
  },
  {
    path: '/SignUp',
    alias: '/signup',
    name: 'SignUp',
    component: () => import(/* webpackChunkName: "auth" */ '../views/SignUp.vue'),
    meta: { title: 'Create account · GIS PhotoShare' },
  },
  {
    path: '/MapRenderer',
    alias: '/Map',
    name: 'MapRenderer',
    component: () => import(/* webpackChunkName: "map" */ '../views/MapRenderer.vue'),
    meta: { title: 'Explore map · GIS PhotoShare' },
  },
  {
    path: '/PhotoUpload',
    name: 'PhotoUpload',
    component: () => import(/* webpackChunkName: "upload" */ '../views/PhotoUpload.vue'),
    meta: { title: 'Share a photo · GIS PhotoShare', requiresAuth: true },
  },
  {
    path: '/MyPhotos',
    alias: '/photos',
    name: 'MyPhotos',
    component: () => import(/* webpackChunkName: "gallery" */ '../views/MyPhotos.vue'),
    meta: { title: 'My photos · GIS PhotoShare', requiresAuth: true },
  },
  {
    path: '/photos/:id/edit',
    name: 'PhotoEdit',
    component: () => import(/* webpackChunkName: "photo" */ '../views/PhotoEdit.vue'),
    meta: { title: 'Edit photo · GIS PhotoShare', requiresAuth: true },
  },
  {
    path: '/photos/:id',
    name: 'PhotoDetail',
    component: () => import(/* webpackChunkName: "photo" */ '../views/PhotoDetail.vue'),
    meta: { title: 'Photo details · GIS PhotoShare' },
  },
  {
    path: '/Privacy',
    alias: '/privacy',
    name: 'Privacy',
    component: () => import(/* webpackChunkName: "policy" */ '../views/Privacy.vue'),
    meta: { title: 'Privacy policy · GIS PhotoShare' },
  },
  {
    path: '/Terms',
    alias: '/terms',
    name: 'Terms',
    component: () => import(/* webpackChunkName: "policy" */ '../views/Terms.vue'),
    meta: { title: 'Terms of use · GIS PhotoShare' },
  },
  {
    path: '*',
    name: 'NotFound',
    component: () => import(/* webpackChunkName: "not-found" */ '../views/NotFound.vue'),
    meta: { title: 'Page not found · GIS PhotoShare' },
  },
]

const router = new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash) return { selector: to.hash, behavior: 'smooth' }
    return { x: 0, y: 0 }
  },
})

router.beforeEach((to, from, next) => {
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  if (requiresAuth && !store.getters.isLoggedIn) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else {
    next()
  }
})

router.afterEach((to) => {
  document.title = (to.meta && to.meta.title) || 'GIS PhotoShare'
})

export default router
