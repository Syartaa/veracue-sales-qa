import { cn } from '@/lib/utils'

/**
 * Processing status of a call (project reference §5).
 * Names match the backend C# enum so the API can send them as strings unchanged.
 */
export type CallStatus =
  | 'Uploaded'
  | 'Transcribing'
  | 'Transcribed'
  | 'Evaluating'
  | 'ReadyForReview'
  | 'Reviewed'
  | 'Failed'

// One style per status, used everywhere (UI spec: "Status chips").
const statusStyles: Record<CallStatus, { label: string; className: string }> = {
  Uploaded: { label: 'Uploaded', className: 'bg-progress text-progress-foreground' },
  Transcribing: { label: 'Transcribing', className: 'bg-progress text-progress-foreground' },
  Transcribed: { label: 'Transcribed', className: 'bg-progress text-progress-foreground' },
  Evaluating: { label: 'Evaluating', className: 'bg-progress text-progress-foreground' },
  ReadyForReview: { label: 'Ready for review', className: 'bg-peach text-peach-foreground' },
  Reviewed: { label: 'Reviewed', className: 'bg-sage text-sage-foreground' },
  Failed: { label: 'Failed', className: 'bg-danger text-danger-foreground' },
}

type StatusChipProps = {
  status: CallStatus
  className?: string
}

export function StatusChip({ status, className }: StatusChipProps) {
  const { label, className: colors } = statusStyles[status]
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold whitespace-nowrap',
        colors,
        className,
      )}
    >
      {label}
    </span>
  )
}
