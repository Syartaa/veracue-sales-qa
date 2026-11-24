import { useEffect } from 'react'

/** Sets the browser tab title, e.g. "Call History · VeraCue" — helps with many open tabs and screen readers. */
export function usePageTitle(title: string | undefined) {
  useEffect(() => {
    document.title = title ? `${title} · VeraCue` : 'VeraCue — Conversation QA'
  }, [title])
}
