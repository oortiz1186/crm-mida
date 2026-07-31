export type StoredUser = {
  id: string
  email: string
  fullName: string
  roles: string[]
  permissions: string[]
}

const tokenKey = 'crm_access_token'
const userKey = 'crm_current_user'
const sessionEvent = 'crm-session-changed'

export function getStoredToken() {
  return sessionStorage.getItem(tokenKey) ?? localStorage.getItem(tokenKey)
}

export function getStoredUser(): StoredUser | null {
  const raw = sessionStorage.getItem(userKey) ?? localStorage.getItem(userKey)
  if (!raw) return null
  try { return JSON.parse(raw) as StoredUser } catch { return null }
}

export function clearStoredSession() {
  sessionStorage.removeItem(tokenKey)
  sessionStorage.removeItem(userKey)
  localStorage.removeItem(tokenKey)
  localStorage.removeItem(userKey)
  window.dispatchEvent(new Event(sessionEvent))
}

export function subscribeToSession(listener: () => void) {
  window.addEventListener(sessionEvent, listener)
  window.addEventListener('storage', listener)
  return () => {
    window.removeEventListener(sessionEvent, listener)
    window.removeEventListener('storage', listener)
  }
}

export function installAuthPersistence() {
  const originalFetch = window.fetch.bind(window)
  window.fetch = async (...args) => {
    const response = await originalFetch(...args)
    const requestUrl = typeof args[0] === 'string' ? args[0] : args[0] instanceof Request ? args[0].url : ''
    if (requestUrl.includes('/api/v1/auth/login') && response.ok) {
      try {
        const payload = await response.clone().json() as { accessToken?: string; user?: StoredUser }
        if (payload.accessToken) sessionStorage.setItem(tokenKey, payload.accessToken)
        if (payload.user) sessionStorage.setItem(userKey, JSON.stringify(payload.user))
        window.dispatchEvent(new Event(sessionEvent))
      } catch {
        // El inicio de sesión original conserva el manejo del error.
      }
    }
    return response
  }
}
