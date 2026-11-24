import { ArrowLeft } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { initials, useSession } from '@/lib/session'
import { usePageTitle } from '@/lib/use-page-title'

type PageHeaderProps = {
  title: string
  description?: string
  /** Small line above the title, e.g. the Overview greeting. */
  eyebrow?: ReactNode
  /** Back link shown above the title on detail pages. */
  back?: { to: string; label: string }
  /** The page's main action(s), shown on the right. */
  actions?: ReactNode
  /** "full" shows the name and email next to the avatar (Overview); "compact" shows only the avatar. */
  profile?: 'compact' | 'full'
}

export function PageHeader({ title, description, eyebrow, back, actions, profile = 'compact' }: PageHeaderProps) {
  const user = useSession()?.user // the signed-in manager, from the session
  usePageTitle(title)

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
        {user && (
          <div className="flex items-center gap-2.5">
            <span
              className="flex size-10 items-center justify-center rounded-full bg-lavender-strong text-sm font-bold text-heading"
              aria-label={`Signed in as ${user.displayName}`}
              title={`${user.displayName} (${user.email})`}
            >
              {initials(user.displayName)}
            </span>
            {profile === 'full' && (
              <span className="hidden leading-tight sm:block">
                <span className="block text-sm font-bold text-heading">{user.displayName}</span>
                <span className="block text-xs text-muted-foreground">{user.email}</span>
              </span>
            )}
          </div>
        )}
      </div>
    </header>
  )
}
