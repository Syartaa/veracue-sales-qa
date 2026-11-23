import { ArrowRight, CircleAlert, FileAudio, RotateCw } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api'
import { formatDuration, formatTime } from '@/lib/format'
import { useCall, useRetryCall, type CallDetail } from './api'
import { StageTiles } from './components/StageTiles'

/**
 * /calls/:callId
 *  - while processing or failed: the Processing status (UI spec screen 5)
 *  - when ready: the Evaluation Review (Phase 5)
 */
export function CallDetailsPage() {
  const { callId } = useParams()
  const { data: call, isPending, isError, error, refetch } = useCall(callId)

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

  if (call.status === 'ReadyForReview' || call.status === 'Reviewed') {
    return (
      <>
        <PageHeader back={{ to: '/calls', label: 'Call History' }} title={call.title} />
        <PlaceholderCard phase={5}>
          <p>Evaluation Review: transcript, scorecard with evidence, audio player and overrides.</p>
        </PlaceholderCard>
      </>
    )
  }

  return <ProcessingView call={call} />
}

function ProcessingView({ call }: { call: CallDetail }) {
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
