import { CircleAlert, CircleCheck } from 'lucide-react'
import { plural } from '@/lib/format'

type TemplateSidePanelProps = {
  criteriaCount: number
  totalPoints: number
  issueCount: number
}

/** Right column of the Template Builder: live summary, scoring rules and writing tips. */
export function TemplateSidePanel({ criteriaCount, totalPoints, issueCount }: TemplateSidePanelProps) {
  return (
    <aside className="space-y-5 lg:sticky lg:top-8">
      <section className="rounded-3xl bg-lavender p-5" aria-labelledby="summary-title">
        <h2 id="summary-title" className="text-lg font-bold">
          Template summary
        </h2>
        <dl className="mt-4 grid grid-cols-2 gap-3">
          <div className="rounded-2xl bg-card p-4">
            <dt className="text-sm text-muted-foreground">Criteria</dt>
            <dd className="text-3xl font-extrabold text-heading">{criteriaCount}</dd>
          </div>
          <div className="rounded-2xl bg-card p-4">
            <dt className="text-sm text-muted-foreground">Total points</dt>
            <dd className="text-3xl font-extrabold text-heading">{totalPoints}</dd>
          </div>
        </dl>
        <p
          aria-live="polite"
          className={
            issueCount > 0
              ? 'mt-3 flex items-center gap-2 rounded-xl bg-danger px-3 py-2 text-sm font-medium text-danger-foreground'
              : 'mt-3 flex items-center gap-2 rounded-xl bg-sage px-3 py-2 text-sm font-medium text-sage-foreground'
          }
        >
          {issueCount > 0 ? (
            <>
              <CircleAlert className="size-4 shrink-0" aria-hidden="true" />
              {plural(issueCount, 'issue')} to fix before saving
            </>
          ) : (
            <>
              <CircleCheck className="size-4 shrink-0" aria-hidden="true" />
              Ready to save
            </>
          )}
        </p>
      </section>

      <section className="rounded-3xl border bg-card p-5" aria-labelledby="scoring-title">
        <h2 id="scoring-title" className="text-lg font-bold">
          How scoring works
        </h2>
        <p className="mt-3 rounded-xl bg-lavender px-3 py-2 text-center text-sm font-semibold text-heading">
          Score = points achieved ÷ points available × 100
        </p>
        <ul className="mt-3 list-disc space-y-1.5 pl-5 text-sm text-muted-foreground">
          <li>The AI suggests points and cites transcript evidence for each criterion.</li>
          <li>
            <strong className="text-heading">Not applicable</strong> criteria are left out of the total.
          </li>
          <li>
            <strong className="text-heading">Needs review</strong> is never counted as a pass — a manager decides.
          </li>
          <li>Past results keep a snapshot, so editing this template won’t change old scores.</li>
        </ul>
      </section>

      <section className="rounded-3xl bg-sage p-5 text-sage-foreground" aria-labelledby="tips-title">
        <h2 id="tips-title" className="text-lg font-bold text-sage-foreground">
          Writing good criteria
        </h2>
        <p className="mt-2 text-sm">
          Say <strong>who</strong> must do it (the agent), <strong>what</strong> counts and what doesn’t. Use max
          points to weight what matters most.
        </p>
      </section>
    </aside>
  )
}
