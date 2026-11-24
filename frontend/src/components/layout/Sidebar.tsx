import { useQueryClient } from '@tanstack/react-query'
import { CloudUpload, Headphones, House, Layers, LogOut, type LucideIcon } from 'lucide-react'
import { NavLink, useNavigate } from 'react-router'
import { clearSession } from '@/lib/session'
import { cn } from '@/lib/utils'
import { Logo } from './Logo'
import { MobileNav } from './MobileNav'

type NavItem = { to: string; label: string; icon: LucideIcon; end?: boolean }

const navItems: NavItem[] = [
  { to: '/', label: 'Overview', icon: House, end: true },
  { to: '/templates', label: 'QA Templates', icon: Layers },
  { to: '/evaluate', label: 'Evaluate Call', icon: CloudUpload },
  { to: '/calls', label: 'Call History', icon: Headphones },
]

const linkBase = 'flex items-center gap-3 rounded-xl px-4 py-2.5 text-sm whitespace-nowrap transition-colors'

/**
 * Navigation for the signed-in workspace.
 * - Desktop (md and up): a fixed column on the left, full height, Sign out at the bottom.
 * - Small screens: a top bar (☰ on the left, logo centred) that opens the same links
 *   in a slide-in panel — see MobileNav.
 */
export function Sidebar() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const signOut = () => {
    clearSession()
    queryClient.clear() // forget all cached calls/templates, so nothing shows after signing out
    navigate('/login', { replace: true })
  }

  return (
    <>
      {/* ---------- Small screens: top bar (☰ left, logo centred) + slide-in menu ---------- */}
      <MobileNav>
        {(close) => (
          <>
            <NavLinks className="mt-6" onNavigate={close} />
            <SignOutButton onClick={signOut} className="mt-auto" />
          </>
        )}
      </MobileNav>

      {/* ---------- Desktop: fixed sidebar ---------- */}
      <aside className="sticky top-0 hidden h-screen w-60 shrink-0 flex-col border-r border-sidebar-border bg-sidebar p-5 md:flex">
        <Logo />
        <NavLinks className="mt-6" />
        <SignOutButton onClick={signOut} className="mt-auto" />
      </aside>
    </>
  )
}

/** The four main links; the active one is highlighted. Used by both the sidebar and the mobile menu. */
function NavLinks({ className, onNavigate }: { className?: string; onNavigate?: () => void }) {
  return (
    <nav aria-label="Main" className={className}>
      <ul className="flex flex-col gap-1">
        {navItems.map(({ to, label, icon: Icon, end }) => (
          <li key={to}>
            <NavLink
              to={to}
              end={end}
              onClick={onNavigate}
              className={({ isActive }) =>
                cn(
                  linkBase,
                  isActive
                    ? 'bg-sidebar-accent font-bold text-sidebar-accent-foreground'
                    : 'font-medium text-sidebar-foreground hover:bg-sidebar-accent/60',
                )
              }
            >
              <Icon className="size-5" aria-hidden="true" />
              {label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}

function SignOutButton({ onClick, className }: { onClick: () => void; className?: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(linkBase, 'font-medium text-sidebar-foreground hover:bg-sidebar-accent/60', className)}
    >
      <LogOut className="size-5" aria-hidden="true" />
      Sign out
    </button>
  )
}
