import Vue from 'vue'
import Vuex from 'vuex'

Vue.use(Vuex)

const AUTH_KEY = 'gis_photo_auth'

function decodeToken(token) {
  if (!token) return null
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const normalised = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalised.padEnd(normalised.length + ((4 - (normalised.length % 4)) % 4), '=')
    const decoded = JSON.parse(window.atob(padded))
    const userIdClaim =
      decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
      decoded.nameid ||
      decoded.sub
    const emailClaim =
      decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
      decoded.email ||
      decoded.unique_name
    if (!userIdClaim && !emailClaim) return null
    return { id: userIdClaim || null, email: emailClaim || null }
  } catch {
    return null
  }
}

function loadStoredAuth() {
  try {
    const raw = localStorage.getItem(AUTH_KEY)
    if (!raw) return { token: null, user: null }
    if (raw.startsWith('{')) {
      const parsed = JSON.parse(raw)
      if (parsed && parsed.token) {
        return { token: parsed.token, user: parsed.user || decodeToken(parsed.token) }
      }
      return { token: null, user: null }
    }
    return { token: raw, user: decodeToken(raw) }
  } catch {
    return { token: null, user: null }
  }
}

const storedAuth = loadStoredAuth()

export default new Vuex.Store({
  state: {
    token: storedAuth.token,
    user: storedAuth.user,
  },
  getters: {
    isLoggedIn: (state) => !!state.token,
    authToken: (state) => state.token,
    currentUser: (state) => state.user,
    displayName: (state) => {
      if (!state.user) return ''
      if (state.user.email) return state.user.email.split('@')[0]
      return 'Account'
    },
    isOwner: (state) => (ownerId) =>
      !!state.user && !!state.user.id && state.user.id === ownerId,
  },
  mutations: {
    SET_AUTH(state, { token, user }) {
      state.token = token
      state.user = user || decodeToken(token)
      if (token) {
        localStorage.setItem(AUTH_KEY, JSON.stringify({ token, user: state.user }))
      } else {
        localStorage.removeItem(AUTH_KEY)
      }
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
      let res
      try {
        res = await fetch(`${baseUrl}/api/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password }),
        })
      } catch {
        throw new Error('Could not connect to the server. Check your connection and try again.')
      }
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.error || 'Sign in failed. Check your email and password.')
      commit('SET_AUTH', { token: data.token, user: { id: data.userId, email: data.email } })
      return data
    },
    async register({ commit }, { email, password }) {
      const baseUrl = process.env.VUE_APP_API_URL || ''
      let res
      try {
        res = await fetch(`${baseUrl}/api/auth/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password }),
        })
      } catch {
        throw new Error('Could not connect to the server. Check your connection and try again.')
      }
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        throw new Error(
          data.error || data.errors?.join?.() || 'Account creation failed. Check your details.'
        )
      }
      commit('SET_AUTH', { token: data.token, user: { id: data.userId, email: data.email } })
      return data
    },
    logout({ commit }) {
      commit('LOGOUT')
    },
  },
  modules: {},
})
