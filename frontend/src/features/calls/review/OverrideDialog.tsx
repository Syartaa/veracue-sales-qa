import { Info } from 'lucide-react'
import { useState, type SubmitEvent } from 'react'
import { FieldError } from '@/components/FieldError'
import { OutcomeChip } from '@/components/OutcomeChip'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/lib/api'
import { formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useOverride, type CriterionResult, type OverrideInput } from '../api'
import type { OverridePreset } from './Scorecard'
import { previewOverride } from './score-preview'

type Decision = OverrideInput['outcome']
type Errors = Partial<Record<'outcome' | 'score' | 'reason' | 'general', string>>

const OUTCOMES: { value: Decision; label: string }[] = [
  { value: 'Met', label: 'Met' },
  { value: 'Partial', label: 'Partial' },
  { value: 'NotMet', label: 'Not met' },
  { value: 'NotApplicable', label: 'Not applicable' },
]
const REASON_MAX = 1000

type OverrideDialogProps = {
  callId: string
  result: CriterionResult
  preset?: OverridePreset
  results: CriterionResult[]
  onClose: () => void
}

/**
 * Override score dialog (UI spec screen 8): record a manager's decision with a required reason.
 * The AI's original judgment is shown at the top and stays on record after saving.
 */
export function OverrideDialog({ callId, result, preset, results, onClose }: OverrideDialogProps) {
  const save = useOverride(callId)

  // Start from the existing override, else the AI's suggestion (unless it was "Needs review").
  const [outcome, setOutcome] = useState<Decision | null>(() => {
    if (preset) return preset
    if (result.finalOutcome) return result.finalOutcome as Decision
    return result.aiOutcome === 'NeedsReview' ? null : result.aiOutcome
  })
  const [scoreText, setScoreText] = useState(() => {
    const start = result.finalOutcome ? result.finalScore : result.aiScore
    return preset === 'NotApplicable' || start === null ? '' : String(start)
  })
  const [reason, setReason] = useState(result.overrideReason ?? '')
  const [errors, setErrors] = useState<Errors>({})

  const isNotApplicable = outcome === 'NotApplicable'
  const score = scoreText.trim() === '' ? null : Number(scoreText)
  const preview = previewOverride(results, result.id, outcome, Number.isInteger(score) ? score : null)

  const chooseOutcome = (next: Decision) => {
    setOutcome(next)
    setErrors((e) => ({ ...e, outcome: undefined, score: undefined }))
    // Helpful defaults: Met -> full points, Not met -> 0, Not applicable -> no score.
    if (next === 'NotApplicable') setScoreText('')
    else if (next === 'Met' && scoreText === '') setScoreText(String(result.maxPoints))
    else if (next === 'NotMet') setScoreText('0')
  }

  const validate = (): Errors => {
    const e: Errors = {}
    if (!outcome) e.outcome = 'Choose an outcome.'
    if (outcome && !isNotApplicable) {
      if (score === null) e.score = 'Enter a score.'
      else if (!Number.isInteger(score) || score < 0 || score > result.maxPoints)
        e.score = `The score must be a whole number from 0 to ${result.maxPoints}.`
    }
    if (!reason.trim()) e.reason = 'A reason is required for every override.'
    else if (reason.trim().length > REASON_MAX) e.reason = `The reason can be at most ${REASON_MAX} characters.`
    return e
  }

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault()
    const found = validate()
    setErrors(found)
    if (Object.keys(found).length > 0 || !outcome) return

    save.mutate(
      { resultId: result.id, outcome, score: isNotApplicable ? null : score, reason: reason.trim(), version: result.version },
      {
        onSuccess: onClose,
        onError: (error) => {
          if (error instanceof ApiError && error.problem?.errors) {
            const server = error.problem.errors
            setErrors({ outcome: server.outcome?.[0], score: server.score?.[0], reason: server.reason?.[0] })
          } else {
            // e.g. 409: changed in another tab, or the call was marked reviewed meanwhile.
            setErrors({ general: error instanceof ApiError ? (error.problem?.detail ?? error.message) : 'Saving failed.' })
          }
        },
      },
    )
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <p className="text-xs font-bold tracking-widest text-primary uppercase">Override score</p>
          <DialogTitle className="text-2xl font-extrabold text-heading">{result.name}</DialogTitle>
          <DialogDescription className="sr-only">
            Record your decision for this criterion. The original AI score stays on record.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} noValidate className="space-y-5">
          {/* What the AI said — read-only */}
          <section className="rounded-2xl bg-lavender/70 p-4" aria-label="AI suggestion">
            <div className="flex items-center justify-between gap-3">
              <span className="text-sm font-semibold text-heading">AI suggestion</span>
              <span className="flex items-center gap-2">
                <OutcomeChip outcome={result.aiOutcome} />
                <span className="font-bold text-heading">
                  {result.aiScore ?? '—'} / {result.maxPoints}
                </span>
              </span>
            </div>
            <p className="mt-2 text-sm">{result.aiExplanation}</p>
            {result.evidence.length > 0 && (
              <p className="mt-2 flex flex-wrap gap-1.5">
                {result.evidence.map((e) => (
                  <span key={e.segmentId} className="rounded-full bg-card px-2.5 py-0.5 text-xs font-bold text-peach-foreground tabular-nums">
                    {formatDuration(e.startMs)}
                  </span>
                ))}
              </p>
            )}
          </section>

          {/* Outcome */}
          <fieldset aria-describedby={errors.outcome ? 'outcome-error' : undefined}>
            <legend className="mb-2 text-sm font-semibold text-heading">Outcome</legend>
            <div className="flex flex-wrap gap-2">
              {OUTCOMES.map((o) => (
                <label
                  key={o.value}
                  className={cn(
                    'flex cursor-pointer items-center gap-2 rounded-xl border px-3.5 py-2.5 text-sm font-medium has-focus-visible:ring-[3px] has-focus-visible:ring-ring/50',
                    outcome === o.value && 'border-primary bg-lavender/60 ring-1 ring-primary',
                  )}
                >
                  <input
                    type="radio"
                    name="outcome"
                    value={o.value}
                    checked={outcome === o.value}
                    onChange={() => chooseOutcome(o.value)}
                    className="size-4 accent-primary"
                  />
                  {o.label}
                </label>
              ))}
            </div>
            <FieldError id="outcome-error" message={errors.outcome} />
          </fieldset>

          {/* Score + live preview */}
          <div>
            <Label htmlFor="override-score" className="mb-2">
              Your score
            </Label>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <Input
                  id="override-score"
                  type="number"
                  inputMode="numeric"
                  min={0}
                  max={result.maxPoints}
                  step={1}
                  value={scoreText}
                  onChange={(e) => {
                    setScoreText(e.target.value)
                    setErrors((er) => ({ ...er, score: undefined }))
                  }}
                  disabled={isNotApplicable}
                  aria-invalid={!!errors.score}
                  aria-describedby={errors.score ? 'score-error' : undefined}
                  className="w-24 text-center text-lg font-bold"
                />
                <span className="text-lg font-semibold text-muted-foreground">/ {result.maxPoints}</span>
              </div>
              <span className="rounded-full bg-sage px-3 py-1 text-sm font-semibold text-sage-foreground" aria-live="polite">
                Call score {fmt(preview.before)} → {fmt(preview.after)}
              </span>
            </div>
            {isNotApplicable && (
              <p className="mt-1.5 text-sm text-muted-foreground">Not applicable criteria are left out of the call score.</p>
            )}
            <FieldError id="score-error" message={errors.score} />
          </div>

          {/* Reason */}
          <div>
            <Label htmlFor="override-reason" className="mb-2">
              Reason <span className="font-normal text-danger-foreground">(required)</span>
            </Label>
            <Textarea
              id="override-reason"
              value={reason}
              onChange={(e) => {
                setReason(e.target.value)
                setErrors((er) => ({ ...er, reason: undefined }))
              }}
              maxLength={REASON_MAX}
              placeholder="Why does this criterion deserve a different score?"
              aria-invalid={!!errors.reason}
              aria-describedby={errors.reason ? 'reason-error' : 'reason-note'}
            />
            <FieldError id="reason-error" message={errors.reason} />
            <p id="reason-note" className="mt-2 flex items-center gap-1.5 text-sm text-muted-foreground">
              <Info className="size-4 shrink-0" aria-hidden="true" />
              The original AI score stays on record next to yours.
            </p>
          </div>

          {errors.general && (
            <p role="alert" className="rounded-xl bg-danger px-3 py-2 text-sm font-medium text-danger-foreground">
              {errors.general}
            </p>
          )}

          <DialogFooter className="border-t pt-4">
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {save.isPending ? 'Saving…' : 'Save override'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function fmt(value: number | null) {
  return value === null ? '—' : `${value}%`
}
