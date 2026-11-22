import { ArrowLeft, Bell } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { currentUser } from '@/lib/current-user'

type PageHeaderProps = {
  title: string
  description?: string
  /** Small line above the title, e.g. the Overview greeting. */
  eyebrow?: ReactNode
  /** Back link shown above the title on detail pages. */
  back?: { to: string; label: string }
  /** The page's main action(s), shown on the right. */
  actions?: ReactNode
  /** "full" shows the bell, name and role (Overview); "compact" shows only the avatar. */
  profile?: 'compact' | 'full'
}

export function PageHeader({ title, description, eyebrow, back, actions, profile = 'compact' }: PageHeaderProps) {
  return (
    <header className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0">
        {back && (
          <Link
            to={back.to}
            className="mb-2 inline-flex items-center gap-1.5 rounded-md text-sm font-medium text-primary hover:underline"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            {back.label}
          </Link>
        )}
        {eyebrow && <p className="text-lg font-medium text-primary md:text-xl">{eyebrow}</p>}
        <h1 className="text-3xl font-extrabold tracking-tight md:text-4xl">{title}</h1>
        {description && <p className="mt-1.5 text-muted-foreground">{description}</p>}
      </div>

      <div className="flex shrink-0 items-center gap-3">
        {actions}
        {profile === 'full' && (
          <button
            type="button"
            aria-label="Notifications"
            className="flex size-10 items-center justify-center rounded-xl border border-border bg-card text-heading shadow-xs hover:bg-accent"
          >
            <Bell className="size-5" aria-hidden="true" />
          </button>
        )}
        <div className="flex items-center gap-2.5">
          <span
            className="flex size-10 items-center justify-center rounded-full bg-lavender-strong text-sm font-bold text-heading"
            aria-label={currentUser.name}
            title={currentUser.name}
          >
            {currentUser.initials}
          </span>
          {profile === 'full' && (
            <span className="hidden leading-tight sm:block">
              <span className="block text-sm font-bold text-heading">{currentUser.name}</span>
              {currentUser.role !== currentUser.name && (
                <span className="block text-xs text-muted-foreground">{currentUser.role}</span>
              )}
            </span>
          )}
        </div>
      </div>
    </header>
  )
}
