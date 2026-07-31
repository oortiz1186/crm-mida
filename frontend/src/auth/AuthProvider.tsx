import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { clearStoredSession, getStoredToken, getStoredUser, subscribeToSession, type StoredUser } from '../session'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  user: StoredUser
}

type AuthContextValue = {
  token: string | null
  user: StoredUser | null
  authenticated: boolean
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  hasPermission: (permission: string) => boolean
  hasAnyPermission: (permissions: string[]) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getStoredToken())
  const [user, setUser] = useState<StoredUser | null>(() => getStoredUser())
  const [loading, setLoading] = useState(false)

  useEffect(() => subscribeToSession(() => {
    setToken(getStoredToken())
    setUser(getStoredUser())
  }), [])

  async function login(email: string, password: string) {
    setLoading(true)
    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) throw new Error('Correo o contraseña incorrectos.')
      const data = await response.json() as LoginResponse
      sessionStorage.setItem('crm_access_token', data.accessToken)
      sessionStorage.setItem('crm_current_user', JSON.stringify(data.user))
      window.dispatchEvent(new Event('crm-session-changed'))
      setToken(data.accessToken)
      setUser(data.user)
    } finally {
      setLoading(false)
    }
  }

  function logout() {
    clearStoredSession()
    setToken(null)
    setUser(null)
  }

  const value = useMemo<AuthContextValue>(() => ({
    token,
    user,
    authenticated: Boolean(token && user),
    loading,
    login,
    logout,
    hasPermission: permission => Boolean(user?.permissions.includes(permission)),
    hasAnyPermission: permissions => permissions.some(permission => user?.permissions.includes(permission)),
  }), [token, user, loading])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth debe utilizarse dentro de AuthProvider.')
  return context
}
