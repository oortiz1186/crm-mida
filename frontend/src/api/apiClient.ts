import { clearStoredSession, getStoredToken } from '../session'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

export async function apiFetch(path: string, init: RequestInit = {}) {
  const token = getStoredToken()
  const headers = new Headers(init.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers })
  if (response.status === 401) {
    clearStoredSession()
    window.location.assign('/')
  }
  return response
}

export async function apiJson<T>(path: string, init: RequestInit = {}) {
  const response = await apiFetch(path, init)
  if (!response.ok) throw new Error(`La solicitud falló con código ${response.status}.`)
  return response.json() as Promise<T>
}
