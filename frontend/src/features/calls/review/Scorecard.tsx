import { Check, CircleAlert, Pencil, Play, Undo2 } from 'lucide-react'
import { OutcomeChip } from '@/components/OutcomeChip'
import { Button } from '@/components/ui/button'
import { formatDate, formatDuration, formatTime } from '@/lib/format'
import { cn } from '@/lib/utils'
import { effectiveOutcome, effectiveScore, type CriterionResult, type Evidence } from '../api'
import { speakerLabel } from './speakers'

export type OverridePreset = 'NotApplicable' | undefined

type ScorecardProps = {
  results: CriterionResult[]
  selectedId: string | null
  onSelect: (id: string | null) => void
  /** Reviewed calls are locked: no action buttons. */
  locked: boolean
  agentSpeaker: number | null
  speakerCount: number
  onEvidenceClick: (evidence: Evidence, result: CriterionResult) => void
  onOverride: (result: CriterionResult, preset?: OverridePreset) => void
  onClearOverride: (result: CriterionResult) => void
  clearingId: string | null
}

export function Scorecard(props: ScorecardProps) {
  const { results, selectedId, onSelect } = props

  /** "Agree" stores nothing (decision): it just moves on to the next criterion. */
  const agree = (index: number) => onSelect(results[index + 1]?.id ?? null)

  return (
    <section aria-labelledby="scorecard-title">
      <h2 id="scorecard-title" className="mb-3 text-lg font-bold">
        Scorecard
      </h2>
      <ol className="space-y-3">
        {results.map((result, index) => (
          <CriterionCard
            key={result.id}
            {...props}
            result={result}
            selected={result.id === selectedId}
            onToggle={() => onSelect(result.id === selectedId ? null : result.id)}
            onAgree={() => agree(index)}
          />
        ))}
      </ol>
    </section>
  )
}

type CardProps = ScorecardProps & {
  result: CriterionResult
  selected: boolean
  onToggle: () => void
  onAgree: () => void
}

function CriterionCard({
  result,
  selected,
  locked,
  agentSpeaker,
  speakerCount,
  onToggle,
  onAgree,
  onEvidenceClick,
  onOverride,
  onClearOverride,
  clearingId,
}: CardProps) {
  const outcome = effectiveOutcome(result)
  const score = effectiveScore(result)
  const overridden = result.finalOutcome !== null
  const pending = outcome === 'NeedsReview'
  const firstEvidence = result.evidence[0]
  // Overridden and pending cards always show their details; others expand when selected.
  const expanded = selected || overridden || pending

  return (
    <li
      className={cn(
        'rounded-3xl border bg-card transition-shadow',
        selected && 'border-primary ring-1 ring-primary',
        pending && !selected && 'border-2 border-dashed border-input',
      )}
    >
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={selected}
        className="flex w-full items-center gap-3 rounded-3xl px-4 py-3.5 text-left"
      >
        <span className="flex min-w-0 flex-1 flex-wrap items-center gap-2">
          <OutcomeChip outcome={outcome} />
          {overridden && (
            <span className="rounded-full bg-lavender-strong px-2.5 py-0.5 text-xs font-semibold text-heading">Overridden</span>
          )}
          <span className="font-semibold text-heading">{result.name}</span>
        </span>
        {firstEvidence && !selected && (
          <span className="hidden rounded-lg bg-lavender px-2 py-0.5 text-xs font-bold text-primary tabular-nums sm:inline">
            {formatDuration(firstEvidence.startMs)}
          </span>
        )}
        <span className="shrink-0 text-right font-bold text-heading tabular-nums">
          {overridden && result.aiScore !== null && (
            <s className="mr-2 text-sm font-medium text-muted-foreground">AI {result.aiScore}/{result.maxPoints}</s>
          )}
          {score ?? '—'}
          <span className="text-muted-foreground">/{result.maxPoints}</span>
        </span>
      </button>

      {expanded && (
        <div className="space-y-3 px-4 pb-4">
          {overridden && (
            <div className="rounded-2xl bg-sage p-3 text-sm text-sage-foreground">
              <p>
                <strong>Manager note:</strong> {result.overrideReason}
              </p>
              <p className="mt-1 text-xs">
                {result.overriddenByName}
                {result.overriddenAt && ` · ${formatDate(result.overriddenAt)} ${formatTime(result.overriddenAt)}`}
              </p>
            </div>
          )}

          {pending && !overridden ? (
            <p className="flex items-start gap-2 rounded-2xl bg-muted p-3 text-sm">
              <CircleAlert className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden="true" />
              <span>{result.aiExplanation}</span>
            </p>
          ) : (
            (selected || overridden) && (
              <div className="rounded-2xl bg-lavender/70 p-3">
                <p className="text-xs font-bold tracking-wide text-primary uppercase">
                  {overridden ? `AI suggested ${labelFor(result.aiOutcome)} ${result.aiScore ?? '—'}/${result.maxPoints}` : 'AI explanation'}
                </p>
                <p className="mt-1 text-sm">{result.aiExplanation}</p>
              </div>
            )
          )}

          {result.evidence.length > 0 && (
            <div className="flex flex-wrap items-center gap-1.5">
              <span className="text-sm text-muted-foreground">Evidence</span>
              {result.evidence.map((evidence) => (
                <button
                  key={evidence.segmentId}
                  type="button"
                  onClick={() => onEvidenceClick(evidence, result)}
                  className={cn(
                    'inline-flex items-center gap-1 rounded-full border px-2.5 py-1 text-xs font-bold tabular-nums transition-colors',
                    selected
                      ? 'border-transparent bg-evidence text-heading hover:brightness-95'
                      : 'border-evidence/60 bg-card text-peach-foreground hover:bg-peach',
                  )}
                  aria-label={`Play evidence at ${formatDuration(evidence.startMs)}`}
                >
                  <Play className="size-3 fill-current" aria-hidden="true" />
                  {formatDuration(evidence.startMs)} · {speakerLabel(evidence.speaker, agentSpeaker, speakerCount)}
                </button>
              ))}
            </div>
          )}

          {!locked && (
            <div className="flex flex-wrap gap-2 border-t pt-3">
              {overridden ? (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onClearOverride(result)}
                  disabled={clearingId === result.id}
                >
                  <Undo2 />
                  {clearingId === result.id ? 'Undoing…' : 'Undo override'}
                </Button>
              ) : pending ? (
                <>
                  <Button size="sm" onClick={() => onOverride(result)}>
                    Set score
                  </Button>
                  <Button variant="outline" size="sm" onClick={() => onOverride(result, 'NotApplicable')}>
                    Mark not applicable
                  </Button>
                </>
              ) : (
                selected && (
                  <>
                    <Button variant="secondary" size="sm" onClick={onAgree} className="bg-sage text-sage-foreground hover:bg-sage/80">
                      <Check />
                      Agree
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => onOverride(result)}>
                      <Pencil />
                      Override score
                    </Button>
                  </>
                )
              )}
            </div>
          )}
        </div>
      )}
    </li>
  )
}

function labelFor(outcome: CriterionResult['aiOutcome']) {
  return { Met: 'Met', Partial: 'Partial', NotMet: 'Not met', NotApplicable: 'Not applicable', NeedsReview: 'Needs review' }[outcome]
}
