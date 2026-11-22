import { cn } from '@/lib/utils'

type ScorePillProps = {
  /** Percentage 0–100, or null when the call has no score yet. */
  value: number | null
  className?: string
}

// Lavender pill for a call score; unscored calls show "—" (UI spec: Call History).
export function ScorePill({ value, className }: ScorePillProps) {
  if (value === null) {
    return (
      <span className={cn('text-muted-foreground', className)} aria-label="Not scored yet">
        —
      </span>
    )
  }

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full bg-lavender px-3 py-1 text-sm font-bold text-heading',
        className,
      )}
    >
      {Math.round(value)}%
    </span>
  )
}
