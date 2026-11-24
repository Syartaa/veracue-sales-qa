import { plural } from '@/lib/format'
import type { Score } from '../api'

/** Original AI score (never changes), current score (after overrides) and the criteria bar. */
export function ScoreStrip({ score }: { score: Score }) {
  const segments = [
    { count: score.metCount, label: 'met', className: 'bg-sage-foreground' },
    { count: score.partialCount, label: 'partial', className: 'bg-evidence' },
    { count: score.notMetCount, label: 'not met', className: 'bg-danger-foreground' },
    { count: score.pendingCount, label: 'needs review', className: 'bg-lavender-strong' },
    { count: score.notApplicableCount, label: 'not applicable', className: 'bg-input' },
  ].filter((s) => s.count > 0)
  const total = segments.reduce((sum, s) => sum + s.count, 0)

  const currentNotes = [
    score.overrideCount > 0 && `${plural(score.overrideCount, 'override')} applied`,
    score.pendingPoints > 0 && `${score.pendingPoints} pts pending`,
  ].filter(Boolean)

  return (
    <section className="mb-5 grid gap-3 md:grid-cols-3" aria-label="Scores">
      <div className="rounded-3xl border bg-card p-5">
        <p className="text-sm text-muted-foreground">Original AI score</p>
        <p className="mt-1 flex items-baseline gap-2">
          <span className="text-4xl font-extrabold text-heading">{percent(score.aiPercent)}</span>
          <span className="text-sm text-muted-foreground">
            {score.aiAchieved} / {score.aiAvailable} pts
          </span>
        </p>
        <p className="text-xs text-muted-foreground">Kept on record, never changed</p>
      </div>

      <div className="rounded-3xl bg-lavender p-5">
        <p className="text-sm text-muted-foreground">Current score (after review)</p>
        <p className="mt-1 flex items-baseline gap-2">
          <span className="text-4xl font-extrabold text-heading">{percent(score.currentPercent)}</span>
          <span className="text-sm text-muted-foreground">
            {score.currentAchieved} / {score.currentAvailable} pts
          </span>
        </p>
        <p className="text-xs text-muted-foreground">{currentNotes.join(' · ') || 'No overrides yet'}</p>
      </div>

      <div className="rounded-3xl border bg-card p-5">
        <p className="text-sm text-muted-foreground">Criteria</p>
        <div className="mt-3 flex h-2.5 overflow-hidden rounded-full bg-muted" role="img"
          aria-label={segments.map((s) => `${s.count} ${s.label}`).join(', ')}>
          {segments.map((s) => (
            <span key={s.label} className={s.className} style={{ width: `${(s.count / total) * 100}%` }} />
          ))}
        </div>
        <p className="mt-3 flex flex-wrap gap-x-3 gap-y-1 text-sm text-muted-foreground">
          {segments.map((s) => (
            <span key={s.label}>
              <strong className="text-heading">{s.count}</strong> {s.label}
            </span>
          ))}
        </p>
      </div>
    </section>
  )
}

function percent(value: number | null) {
  return value === null ? '—' : `${value}%`
}
