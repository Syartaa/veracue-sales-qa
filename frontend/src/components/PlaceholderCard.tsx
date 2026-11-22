import { Construction } from 'lucide-react'
import type { ReactNode } from 'react'

type PlaceholderCardProps = {
  phase: number
  children: ReactNode
}

// Temporary: marks a page that is routed but not built yet. Removed as each page is implemented.
export function PlaceholderCard({ phase, children }: PlaceholderCardProps) {
  return (
    <section className="rounded-3xl border-2 border-dashed border-input bg-card/60 p-8">
      <div className="flex items-start gap-4">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-lavender text-primary">
          <Construction className="size-5" aria-hidden="true" />
        </span>
        <div className="space-y-1.5 text-muted-foreground">
          <p className="font-semibold text-heading">Built in Phase {phase}</p>
          {children}
        </div>
      </div>
    </section>
  )
}
