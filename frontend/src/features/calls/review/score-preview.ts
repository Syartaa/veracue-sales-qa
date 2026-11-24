import { effectiveOutcome, effectiveScore, type CriterionOutcome, type CriterionResult } from '../api'

/**
 * The same formula as the backend's ScoreCalculator, used ONLY for the dialog's live preview
 * ("Call score 88% → 91%"). The number that is saved and shown afterwards always comes from
 * the server.
 */
export function currentPercent(results: { outcome: CriterionOutcome; score: number | null; maxPoints: number }[]) {
  const scored = results.filter((r) => r.outcome === 'Met' || r.outcome === 'Partial' || r.outcome === 'NotMet')
  const available = scored.reduce((sum, r) => sum + r.maxPoints, 0)
  if (available === 0) return null
  const achieved = scored.reduce((sum, r) => sum + (r.score ?? 0), 0)
  return Math.round((achieved * 100) / available) // Math.round rounds .5 up, like the backend
}

/** Current score now, and what it would be if `resultId` got the given decision. */
export function previewOverride(
  results: CriterionResult[],
  resultId: string,
  outcome: CriterionOutcome | null,
  score: number | null,
) {
  const now = results.map((r) => ({ outcome: effectiveOutcome(r), score: effectiveScore(r), maxPoints: r.maxPoints }))
  const after = results.map((r) =>
    r.id === resultId && outcome
      ? { outcome, score: outcome === 'NotApplicable' ? null : score, maxPoints: r.maxPoints }
      : { outcome: effectiveOutcome(r), score: effectiveScore(r), maxPoints: r.maxPoints },
  )
  return { before: currentPercent(now), after: currentPercent(after) }
}
