import { ErrorState } from '@/components/ErrorState'
import { OutcomeChip } from '@/components/OutcomeChip'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { formatDuration } from '@/lib/format'
import { useCallReview } from '../api'

/**
 * TEMPORARY (Phase 4): a simple read-only list of the AI results, so evaluations can be
 * checked in the browser. Phase 5 replaces it with the full Evaluation Review screen.
 */
export function ResultsPreview({ callId }: { callId: string }) {
  const { data: review, isPending, isError, error, refetch } = useCallReview(callId)

  if (isPending) return <div className="h-96 animate-pulse rounded-3xl border bg-card" aria-busy="true" />
  if (isError) return <ErrorState title="Couldn't load the results" error={error} onRetry={() => refetch()} />

  const evaluation = review.evaluation
  if (!evaluation) return <p className="text-muted-foreground">This call hasn’t been evaluated yet.</p>
  const { score } = evaluation

  return (
    <div className="space-y-5">
      <div className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-3xl border bg-card p-5">
          <p className="text-sm text-muted-foreground">Original AI score</p>
          <p className="text-3xl font-extrabold text-heading">
            {score.aiPercent ?? '—'}
            {score.aiPercent !== null && '%'}
            <span className="ml-2 text-base font-medium text-muted-foreground">
              {score.aiAchieved} / {score.aiAvailable} pts
            </span>
          </p>
        </div>
        <div className="rounded-3xl bg-lavender p-5">
          <p className="text-sm text-muted-foreground">Current score</p>
          <p className="text-3xl font-extrabold text-heading">
            {score.currentPercent ?? '—'}
            {score.currentPercent !== null && '%'}
          </p>
          {score.pendingCount > 0 && (
            <p className="text-sm text-muted-foreground">{score.pendingPoints} pts pending</p>
          )}
        </div>
        <div className="rounded-3xl border bg-card p-5 text-sm text-muted-foreground">
          <p>{score.metCount} met · {score.partialCount} partial · {score.notMetCount} not met</p>
          <p>{score.pendingCount} needs review · {score.notApplicableCount} not applicable</p>
          <p className="mt-1 text-xs">Model {evaluation.modelName} · prompt {evaluation.promptVersion}</p>
        </div>
      </div>

      <ol className="space-y-3">
        {evaluation.results.map((result) => (
          <li key={result.id} className="rounded-3xl border bg-card p-5">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2">
                <OutcomeChip outcome={result.aiOutcome} />
                <span className="font-semibold text-heading">{result.name}</span>
              </div>
              <span className="font-bold text-heading">
                {result.aiScore ?? '—'}
                <span className="text-muted-foreground">/{result.maxPoints}</span>
              </span>
            </div>
            <p className="mt-2 text-sm">{result.aiExplanation}</p>
            {result.evidence.length > 0 && (
              <p className="mt-2 flex flex-wrap gap-1.5">
                {result.evidence.map((e) => (
                  <span key={e.segmentId} className="rounded-full bg-peach px-2.5 py-0.5 text-xs font-semibold text-peach-foreground">
                    {formatDuration(e.startMs)} · Speaker {e.speaker}
                  </span>
                ))}
              </p>
            )}
          </li>
        ))}
      </ol>

      <PlaceholderCard phase={5}>
        <p>Full Evaluation Review: audio player, transcript with highlighted evidence, and manager overrides.</p>
      </PlaceholderCard>
    </div>
  )
}
