import { cn } from '@/lib/utils'
import type { CriterionOutcome } from '@/features/calls/api'

// Criterion outcome chips use the status palette (UI spec): Met = sage, Partial = peach,
// Needs review = lavender, Not met = error, Not applicable = neutral.
const styles: Record<CriterionOutcome, { label: string; className: string }> = {
  Met: { label: 'Met', className: 'bg-sage text-sage-foreground' },
  Partial: { label: 'Partial', className: 'bg-peach text-peach-foreground' },
  NotMet: { label: 'Not met', className: 'bg-danger text-danger-foreground' },
  NotApplicable: { label: 'Not applicable', className: 'bg-muted text-muted-foreground' },
  NeedsReview: { label: 'Needs review', className: 'bg-progress text-progress-foreground' },
}

export function OutcomeChip({ outcome, className }: { outcome: CriterionOutcome; className?: string }) {
  const { label, className: colors } = styles[outcome]
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap',
        colors,
        className,
      )}
    >
      {label}
    </span>
  )
}
