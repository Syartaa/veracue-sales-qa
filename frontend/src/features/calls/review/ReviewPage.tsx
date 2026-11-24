import { useRef, useState } from 'react'
import { ErrorState } from '@/components/ErrorState'
import { ApiError } from '@/lib/api'
import {
  effectiveOutcome,
  useCallReview,
  useClearOverride,
  useMarkReviewed,
  type CriterionResult,
  type Evidence,
  type TranscriptSegment,
} from '../api'
import { AudioPlayer, type AudioPlayerHandle } from './AudioPlayer'
import { OverrideDialog } from './OverrideDialog'
import { ReviewHeader } from './ReviewHeader'
import { Scorecard, type OverridePreset } from './Scorecard'
import { ScoreStrip } from './ScoreStrip'
import { TranscriptPanel } from './TranscriptPanel'

/**
 * Evaluation Details / Manager Review (UI spec screen 7) — the main screen:
 * verify each AI judgment against the call, and correct it.
 */
export function ReviewPage({ callId }: { callId: string }) {
  const { data: review, isPending, isError, error, refetch } = useCallReview(callId)
  const clearOverride = useClearOverride(callId)
  const markReviewed = useMarkReviewed(callId)

  const playerRef = useRef<AudioPlayerHandle>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [activeSegmentId, setActiveSegmentId] = useState<string | null>(null)
  const [overrideTarget, setOverrideTarget] = useState<{ result: CriterionResult; preset?: OverridePreset } | null>(null)

  if (isPending) return <div className="h-[32rem] animate-pulse rounded-3xl border bg-card" aria-busy="true" aria-label="Loading review" />
  if (isError) return <ErrorState title="Couldn't load the review" error={error} onRetry={() => refetch()} />

  const evaluation = review.evaluation
  if (!evaluation) return <p className="text-muted-foreground">This call hasn’t been evaluated yet.</p>

  // Default selection: the first criterion still waiting for a decision, otherwise the first one.
  const selected =
    evaluation.results.find((r) => r.id === selectedId) ??
    (selectedId === null ? (evaluation.results.find((r) => effectiveOutcome(r) === 'NeedsReview') ?? null) : null)

  const locked = review.call.status !== 'ReadyForReview'

  // Clicking a timestamp or an evidence chip: highlight that line and play the audio from there.
  const goToSegment = (segmentId: string, startMs: number) => {
    setActiveSegmentId(segmentId)
    playerRef.current?.seekTo(startMs)
  }
  const handleSegmentClick = (segment: TranscriptSegment) => goToSegment(segment.id, segment.startMs)
  const handleEvidenceClick = (evidence: Evidence, result: CriterionResult) => {
    setSelectedId(result.id)
    goToSegment(evidence.segmentId, evidence.startMs)
  }

  // While the audio plays, the transcript follows: highlight the line being spoken.
  const handleTimeChange = (ms: number) => {
    const current = review.transcript.findLast((s) => s.startMs <= ms)
    if (current && current.id !== activeSegmentId) setActiveSegmentId(current.id)
  }

  const actionError = [clearOverride.error, markReviewed.error].find(Boolean)
  const actionMessage =
    actionError instanceof ApiError ? (actionError.problem?.detail ?? actionError.message) : actionError ? 'Something went wrong.' : undefined

  return (
    <>
      <ReviewHeader
        review={review}
        onMarkReviewed={() => markReviewed.mutate(undefined)}
        marking={markReviewed.isPending}
        error={actionMessage}
      />

      <ScoreStrip score={evaluation.score} />

      <AudioPlayer
        ref={playerRef}
        callId={callId}
        durationMs={review.call.durationMs}
        results={evaluation.results}
        selectedId={selected?.id ?? null}
        onTimeChange={handleTimeChange}
      />

      <div className="grid items-start gap-5 lg:grid-cols-2 [&>*]:min-w-0">
        <TranscriptPanel
          transcript={review.transcript}
          agentSpeaker={review.agentSpeaker}
          speakerCount={review.call.speakerCount}
          selected={selected}
          activeSegmentId={activeSegmentId}
          onSegmentClick={handleSegmentClick}
        />
        <Scorecard
          results={evaluation.results}
          selectedId={selected?.id ?? null}
          onSelect={setSelectedId}
          locked={locked}
          agentSpeaker={review.agentSpeaker}
          speakerCount={review.call.speakerCount}
          onEvidenceClick={handleEvidenceClick}
          onOverride={(result, preset) => setOverrideTarget({ result, preset })}
          onClearOverride={(result) => clearOverride.mutate({ resultId: result.id, version: result.version })}
          clearingId={clearOverride.isPending ? (clearOverride.variables?.resultId ?? null) : null}
        />
      </div>

      {overrideTarget && (
        <OverrideDialog
          key={overrideTarget.result.id}
          callId={callId}
          result={overrideTarget.result}
          preset={overrideTarget.preset}
          results={evaluation.results}
          onClose={() => setOverrideTarget(null)}
        />
      )}
    </>
  )
}
