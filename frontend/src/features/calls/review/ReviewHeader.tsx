import { ArrowLeft, Check } from 'lucide-react'
import { Link } from 'react-router'
import { StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'
import { formatDate, formatDuration, formatTime, plural } from '@/lib/format'
import type { CallReview } from '../api'

type ReviewHeaderProps = {
  review: CallReview
  onMarkReviewed: () => void
  marking: boolean
  error?: string
}

/** Back link, title, status, info chips and the "Mark as reviewed" action. */
export function ReviewHeader({ review, onMarkReviewed, marking, error }: ReviewHeaderProps) {
  const { call, evaluation } = review
  const score = evaluation?.score
  const reviewed = call.status === 'Reviewed'

  const chips = [
    evaluation?.templateName ?? call.templateName,
    `${formatDate(call.uploadedAt)} · ${formatTime(call.uploadedAt)}`,
    call.durationMs ? formatDuration(call.durationMs) : null,
    plural(call.speakerCount, 'speaker'),
  ].filter(Boolean)

  return (
    <header className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <div className="min-w-0">
        <Link to="/calls" className="mb-2 inline-flex items-center gap-1.5 text-sm font-medium text-primary hover:underline">
          <ArrowLeft className="size-4" aria-hidden="true" />
          Call History
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-3xl font-extrabold tracking-tight md:text-4xl">{call.title}</h1>
          <StatusChip status={call.status} />
        </div>
        <ul className="mt-3 flex flex-wrap gap-2" aria-label="Call details">
          {chips.map((chip) => (
            <li key={chip} className="rounded-full border bg-card px-3 py-1 text-sm text-heading">
              {chip}
            </li>
          ))}
        </ul>
      </div>

      <div className="flex shrink-0 flex-col items-start gap-1.5 lg:items-end">
        {reviewed ? (
          <p className="flex items-center gap-2 rounded-xl bg-sage px-4 py-2.5 text-sm font-semibold text-sage-foreground">
            <Check className="size-4" aria-hidden="true" />
            Reviewed{evaluation?.reviewedByName ? ` by ${evaluation.reviewedByName}` : ''}
            {evaluation?.reviewedAt ? ` · ${formatDate(evaluation.reviewedAt)}` : ''}
          </p>
        ) : (
          <>
            <Button size="lg" onClick={onMarkReviewed} disabled={!score?.canMarkReviewed || marking}>
              <Check />
              {marking ? 'Saving…' : 'Mark as reviewed'}
            </Button>
            {score && !score.canMarkReviewed && (
              <p className="text-sm font-medium text-peach-foreground">
                Decide {plural(score.pendingCount, 'pending criterion', 'pending criteria')} first
              </p>
            )}
          </>
        )}
        {error && <p className="text-sm font-medium text-danger-foreground">{error}</p>}
      </div>
    </header>
  )
}
