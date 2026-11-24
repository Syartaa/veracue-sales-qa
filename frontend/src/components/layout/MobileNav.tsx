import { Menu, X } from 'lucide-react'
import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import { useLocation } from 'react-router'
import { cn } from '@/lib/utils'
import { Logo } from './Logo'

/**
 * Small-screen navigation: a top bar (☰ on the left, logo centred) and a menu panel that
 * slides in from the left.
 *
 * Deliberately simple and self-contained: the panel only moves with a CSS transform, so the
 * page behind never changes width while it opens or closes (no layout jumps). It still behaves
 * like a proper modal menu: Esc or tapping outside closes it, keyboard focus stays inside while
 * it's open and returns to the ☰ button afterwards, and the page behind can't be scrolled.
 */
export function MobileNav({ children }: { children: (close: () => void) => ReactNode }) {
  const [open, setOpen] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const toggleRef = useRef<HTMLButtonElement>(null)
  const location = useLocation()

  const close = () => setOpen(false)

  // Close when the page changes (e.g. after tapping a link).
  const [lastPath, setLastPath] = useState(location.pathname)
  if (location.pathname !== lastPath) {
    setLastPath(location.pathname)
    setOpen(false)
  }

  useEffect(() => {
    if (!open) return
    // Stop the page behind from scrolling (no width compensation needed: mobile scrollbars overlay).
    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    // Move focus into the menu so keyboard and screen-reader users land in it.
    panelRef.current?.querySelector<HTMLElement>('a, button')?.focus()
    // Close if the window grows to desktop size (the sidebar takes over there).
    const desktop = window.matchMedia('(min-width: 768px)')
    const onResize = () => desktop.matches && setOpen(false)
    desktop.addEventListener('change', onResize)

    const toggle = toggleRef.current
    return () => {
      document.body.style.overflow = previous
      desktop.removeEventListener('change', onResize)
      toggle?.focus() // back to ☰ when the menu closes
    }
  }, [open])

  // Esc closes; Tab / Shift+Tab cycle inside the panel instead of leaving it.
  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Escape') {
      close()
      return
    }
    if (event.key !== 'Tab' || !panelRef.current) return
    const focusable = [...panelRef.current.querySelectorAll<HTMLElement>('a, button')]
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last?.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first?.focus()
    }
  }

  return (
    <>
      <header className="sticky top-0 z-30 grid grid-cols-[2.75rem_1fr_2.75rem] items-center border-b border-sidebar-border bg-sidebar px-3 py-2.5 md:hidden">
        <button
          ref={toggleRef}
          type="button"
          onClick={() => setOpen(true)}
          aria-label="Open menu"
          aria-expanded={open}
          aria-controls="mobile-menu"
          className="flex size-11 items-center justify-center rounded-xl text-heading transition-colors hover:bg-sidebar-accent focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
        >
          <Menu className="size-6" aria-hidden="true" />
        </button>
        <div className="flex justify-center">
          <Logo />
        </div>
        <span aria-hidden="true" /> {/* balances the ☰ button so the logo is truly centred */}
      </header>

      {/* Dimmed backdrop: fades in/out; tapping it closes the menu. */}
      <div
        aria-hidden="true"
        onClick={close}
        className={cn(
          'fixed inset-0 z-40 bg-heading/40 transition-opacity duration-300 motion-reduce:transition-none md:hidden',
          open ? 'opacity-100' : 'pointer-events-none opacity-0',
        )}
      />

      {/* The panel stays mounted and slides with transform only, so opening/closing is smooth. */}
      <div
        id="mobile-menu"
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label="Menu"
        inert={!open} // when closed: not focusable, hidden from screen readers
        onKeyDown={onKeyDown}
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-72 max-w-[85vw] flex-col bg-sidebar p-5 shadow-xl md:hidden',
          'transition-transform duration-300 ease-out will-change-transform motion-reduce:transition-none',
          open ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex items-center justify-between">
          <Logo />
          <button
            type="button"
            onClick={close}
            aria-label="Close menu"
            className="flex size-11 items-center justify-center rounded-xl text-heading transition-colors hover:bg-sidebar-accent focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        </div>
        {children(close)}
      </div>
    </>
  )
}
