import Vue from 'vue'
import Vuex from 'vuex'

Vue.use(Vuex)

const AUTH_KEY = 'gis_photo_auth'

export default new Vuex.Store({
  state: {
    token: localStorage.getItem(AUTH_KEY) || null,
    user: null,
  },
  getters: {
    isLoggedIn: (s) => !!s.token,
    authToken: (s) => s.token,
  },
  mutations: {
    SET_AUTH(state, { token, user }) {
      state.token = token
      state.user = user
      if (token) localStorage.setItem(AUTH_KEY, token)
      else localStorage.removeItem(AUTH_KEY)
    },
    LOGOUT(state) {
      state.token = null
      state.user = null
      localStorage.removeItem(AUTH_KEY)
    },
  },
  actions: {
    async login({ commit }, { email, password }) {
      const baseUrl = process.env.VUE_APP_API_URL || ''
      const res = await fetch(`${baseUrl}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.error || 'Login failed')
      commit('SET_AUTH', { token: data.token, user: { id: data.userId, email: data.email } })
      return data
    },
    async register({ commit }, { email, password }) {
      const baseUrl = process.env.VUE_APP_API_URL || ''
      const res = await fetch(`${baseUrl}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.error || data.errors?.join?.() || 'Registration failed')
      commit('SET_AUTH', { token: data.token, user: { id: data.userId, email: data.email } })
      return data
    },
    logout({ commit }) {
      commit('LOGOUT')
    },
  },
  modules: {},
})
