import { useSyncExternalStore } from 'react'

/**
 * The signed-in session: the token from POST /api/auth/login plus who it belongs to.
 * Kept in localStorage so it survives page reloads and is shared by open tabs.
 * (Trade-off, noted in the write-up: a script injected into the page could read it.
 * Fine for a single-account demo; production would use same-site cookies.)
 */
export type SessionUser = { id: string; email: string; displayName: string }
export type Session = { token: string; expiresAt: string; user: SessionUser }

const STORAGE_KEY = 'veracue.session'
const CHANGED = 'veracue:session-changed'

let cachedRaw: string | null | undefined
let cachedSession: Session | null = null

function read(): Session | null {
  let raw: string | null = null
  try {
    raw = localStorage.getItem(STORAGE_KEY)
  } catch {
    raw = null // storage blocked (e.g. private mode): behave as signed out
  }
  if (raw === cachedRaw) return cachedSession // same object while nothing changed (needed by React)
  cachedRaw = raw
  try {
    const parsed = raw ? (JSON.parse(raw) as Session) : null
    cachedSession = parsed && new Date(parsed.expiresAt).getTime() > Date.now() ? parsed : null
  } catch {
    cachedSession = null
  }
  return cachedSession
}

export function getSession() {
  return read()
}

export function saveSession(session: Session) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
  } catch {
    /* storage blocked: the session lasts until the page is closed */
  }
  window.dispatchEvent(new Event(CHANGED))
}

export function clearSession() {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    /* ignore */
  }
  window.dispatchEvent(new Event(CHANGED))
}

function subscribe(onChange: () => void) {
  window.addEventListener(CHANGED, onChange)
  window.addEventListener('storage', onChange) // sign-in / sign-out in another tab
  return () => {
    window.removeEventListener(CHANGED, onChange)
    window.removeEventListener('storage', onChange)
  }
}

/** The current session (or null), re-rendering whenever it changes — in this tab or another. */
export function useSession() {
  return useSyncExternalStore(subscribe, read, () => null)
}

/** "Manager" -> "M", "Quality Lead" -> "QL" */
export function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
}
