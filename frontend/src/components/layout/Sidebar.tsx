import { useQueryClient } from '@tanstack/react-query'
import { CloudUpload, Headphones, House, Layers, LogOut, type LucideIcon } from 'lucide-react'
import { NavLink, useNavigate } from 'react-router'
import { clearSession } from '@/lib/session'
import { cn } from '@/lib/utils'
import { Logo } from './Logo'

type NavItem = { to: string; label: string; icon: LucideIcon; end?: boolean }

const navItems: NavItem[] = [
  { to: '/', label: 'Overview', icon: House, end: true },
  { to: '/templates', label: 'QA Templates', icon: Layers },
  { to: '/evaluate', label: 'Evaluate Call', icon: CloudUpload },
  { to: '/calls', label: 'Call History', icon: Headphones },
]

const linkBase =
  'flex items-center gap-3 rounded-xl px-4 py-2.5 text-sm whitespace-nowrap transition-colors'

/*
 * Desktop (md+): fixed-width column on the left, full height, Sign out at the bottom.
 * Mobile: stacks above the content; nav links become a horizontally scrolling row.
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
    <aside className="flex shrink-0 flex-wrap items-center gap-4 border-b border-sidebar-border bg-sidebar p-4 md:sticky md:top-0 md:h-screen md:w-60 md:flex-col md:flex-nowrap md:items-stretch md:border-r md:border-b-0 md:p-5">
      <Logo />

      <nav aria-label="Main" className="order-last w-full md:order-none md:mt-6">
        <ul className="flex gap-1 overflow-x-auto md:flex-col">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <li key={to}>
              <NavLink
                to={to}
                end={end}
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

      <button
        type="button"
        onClick={signOut}
        className={cn(linkBase, 'ml-auto font-medium text-sidebar-foreground hover:bg-sidebar-accent/60 md:mt-auto md:ml-0')}
      >
        <LogOut className="size-5" aria-hidden="true" />
        Sign out
      </button>
    </aside>
  )
}
