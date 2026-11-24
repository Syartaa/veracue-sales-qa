import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useSession } from '@/lib/session'

/**
 * Route guard for the signed-in workspace. Without a valid session (never signed in,
 * signed out, token expired or rejected by the API) it sends the user to /login and
 * remembers where they were, so signing in brings them straight back.
 * The API enforces the same rule independently — this is about a smooth experience.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const session = useSession()
  const location = useLocation()

  if (!session) {
    const returnTo = location.pathname + location.search
    return <Navigate to={`/login?returnTo=${encodeURIComponent(returnTo)}`} replace />
  }

  return children
}
