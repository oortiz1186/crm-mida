import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './AuthProvider'

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { authenticated } = useAuth()
  const location = useLocation()

  if (!authenticated) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />
  }

  return <>{children}</>
}
