import { Plus } from 'lucide-react'
import { Link } from 'react-router'

/** Dashed "Create a template" tile at the end of the grid. */
export function CreateTemplateCard() {
  return (
    <Link
      to="/templates/new"
      className="flex min-h-64 flex-col items-center justify-center rounded-3xl border-2 border-dashed border-input bg-card/50 p-6 text-center transition-colors hover:border-primary/40 hover:bg-accent/40"
    >
      <span className="flex size-12 items-center justify-center rounded-full bg-lavender text-primary">
        <Plus className="size-6" aria-hidden="true" />
      </span>
      <span className="mt-4 font-bold text-heading">Create a template</span>
      <span className="mt-1 text-sm text-muted-foreground">Start from scratch with your own criteria</span>
    </Link>
  )
}
