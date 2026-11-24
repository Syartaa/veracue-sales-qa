import { ArrowRight, CircleAlert, FileAudio, RotateCw } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api'
import { formatDuration, formatTime } from '@/lib/format'
import { useCall, useRetryCall, type CallDetail } from './api'
import { StageTiles } from './components/StageTiles'
import { ReviewPage } from './review/ReviewPage'

/**
 * /calls/:callId
 *  - while processing or failed: the Processing status (UI spec screen 5)
 *  - when it finishes while you watch: the "Ready" card with "Open review"
 *  - when opened for an already-finished call: the review straight away
 */
export function CallDetailsPage() {
  const { callId } = useParams()
  const { data: call, isPending, isError, error, refetch } = useCall(callId)
  const [sawProcessing, setSawProcessing] = useState(false)
  const [reviewOpened, setReviewOpened] = useState(false)

  const isFinished = call?.status === 'ReadyForReview' || call?.status === 'Reviewed'
  // Remember that this visit started while the call was still processing (or failed),
  // so finishing shows the "Ready" card instead of jumping away from the progress view.
  if (call && !isFinished && !sawProcessing) setSawProcessing(true)

  if (isPending) {
    return (
      <>
        <PageHeader back={{ to: '/calls', label: 'Call History' }} title="Processing your call" />
        <div className="h-72 animate-pulse rounded-3xl border bg-card" aria-busy="true" aria-label="Loading call" />
      </>
    )
  }

  if (isError) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <>
        <PageHeader back={{ to: '/calls', label: 'Call History' }} title="Call not found" />
        {notFound ? (
          <section className="rounded-3xl border bg-card p-8 text-center">
            <p className="font-semibold text-heading">This call doesn’t exist.</p>
            <Button asChild variant="outline" className="mt-4">
              <Link to="/evaluate">Evaluate a call</Link>
            </Button>
          </section>
        ) : (
          <ErrorState title="Couldn't load the call" error={error} onRetry={() => refetch()} />
        )}
      </>
    )
  }

  if (isFinished && (reviewOpened || !sawProcessing)) return <ReviewPage callId={call.id} />


  return <ProcessingView call={call} onOpenReview={() => setReviewOpened(true)} />
}

function ProcessingView({ call, onOpenReview }: { call: CallDetail; onOpenReview: () => void }) {
  const retry = useRetryCall(call.id)
  const failed = call.status === 'Failed'
  const stageName = call.failedStage === 'Evaluation' ? 'evaluation' : 'transcription'

  const facts = [
    call.templateName,
    call.durationMs ? formatDuration(call.durationMs) : null,
    `started ${formatTime(call.uploadedAt)}`,
  ].filter(Boolean)

  return (
    <>
      <PageHeader
        back={{ to: '/calls', label: 'Call History' }}
        title="Processing your call"
        description="Each stage is saved as it finishes, so a failure never means starting over."
      />

      <section
        className="space-y-5 rounded-3xl border bg-card p-5 shadow-xs sm:p-6"
        aria-labelledby="call-title"
        aria-live="polite"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex min-w-0 items-start gap-3">
            <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-lavender text-primary">
              <FileAudio className="size-5" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <h2 id="call-title" className="truncate text-lg font-bold">
                {call.title}
              </h2>
              <p className="text-sm text-muted-foreground">{facts.join(' · ')}</p>
            </div>
          </div>
          <StatusChip status={call.status} />
        </div>

        <StageTiles call={call} />

        {failed && (
          <div className="space-y-3 rounded-2xl border border-danger-foreground/20 bg-danger p-4">
            <p className="flex items-start gap-2 text-sm text-danger-foreground">
              <CircleAlert className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
              <span>
                <strong>{call.failedStage === 'Evaluation' ? 'Evaluation' : 'Transcription'} stopped:</strong>{' '}
                {call.errorMessage ?? 'Something went wrong.'}
                {call.failedStage === 'Evaluation' && ' The transcript is saved, so retrying won’t transcribe the call again.'}
              </span>
            </p>
            <Button onClick={() => retry.mutate()} disabled={retry.isPending}>
              <RotateCw className={retry.isPending ? 'animate-spin' : undefined} />
              {retry.isPending ? 'Retrying…' : `Retry ${stageName}`}
            </Button>
            {retry.isError && <p className="text-sm text-danger-foreground">Couldn’t start the retry. Try again.</p>}
          </div>
        )}

        {call.status === 'ReadyForReview' && (
          <div className="flex flex-col gap-4 rounded-2xl border border-peach-foreground/15 bg-peach/60 p-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="flex items-baseline gap-3">
                <span className="text-4xl font-extrabold text-heading">
                  {call.aiScorePercent !== null ? `${call.aiScorePercent}%` : '—'}
                </span>
                <span className="text-sm text-muted-foreground">
                  AI score
                  {call.pendingCount > 0 &&
                    ` · ${call.pendingCount} ${call.pendingCount === 1 ? 'criterion needs' : 'criteria need'} your decision`}
                </span>
              </p>
              <p className="mt-1 text-sm text-foreground">
                All stages finished. Check the evidence and confirm or override each score.
              </p>
            </div>
            <Button onClick={onOpenReview} size="lg" className="shrink-0">
              Open review
              <ArrowRight />
            </Button>
          </div>
        )}

        <div className="flex flex-col gap-3 border-t pt-4 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            You can leave this page — processing continues and the result will appear in Call History.
          </p>
          <div className="flex shrink-0 gap-2">
            <Button asChild variant="outline">
              <Link to="/calls">Go to Call History</Link>
            </Button>
            <Button asChild variant="secondary">
              <Link to="/evaluate">
                Evaluate another call
                <ArrowRight />
              </Link>
            </Button>
          </div>
        </div>
      </section>
    </>
  )
}
