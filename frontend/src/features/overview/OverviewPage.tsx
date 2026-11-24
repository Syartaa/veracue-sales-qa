import { ArrowRight, Clock, CloudUpload, Headphones, Layers, Play, Target, type LucideIcon } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { ScorePill } from '@/components/ScorePill'
import { StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'
import { formatDate, formatDuration } from '@/lib/format'
import { useOverview, type Overview } from './api'

function greeting(hour: number) {
  if (hour < 12) return 'Good morning'
  if (hour < 18) return 'Good afternoon'
  return 'Good evening'
}

/** Overview (UI spec screen 1): a welcome page with a quick way to start an evaluation. */
export function OverviewPage() {
  // Computed once when the page mounts, not on every re-render.
  const [hello] = useState(() => greeting(new Date().getHours()))
  const { data, isPending, isError, error, refetch } = useOverview()

  return (
    <>
      <PageHeader
        eyebrow={hello}
        title="Welcome to VeraCue"
        description="Evaluate sales calls, verify results with real evidence, and keep every customer conversation on standard."
        profile="full"
      />

      {isPending ? (
        <div className="space-y-5" aria-busy="true" aria-label="Loading overview">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {[0, 1, 2, 3].map((i) => <div key={i} className="h-32 animate-pulse rounded-3xl bg-card" />)}
          </div>
          <div className="h-80 animate-pulse rounded-3xl bg-card" />
        </div>
      ) : isError ? (
        <ErrorState title="Couldn't load the overview" error={error} onRetry={() => refetch()} />
      ) : (
        <>
          <SummaryCards data={data} />
          <div className="mt-5 grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_360px] [&>*]:min-w-0">
            <RecentEvaluations recent={data.recent} />
            <CallToAction />
          </div>
        </>
      )}
    </>
  )
}

function SummaryCards({ data }: { data: Overview }) {
  const cards: { label: string; value: string; note: string; icon: LucideIcon; className: string; iconClass: string }[] = [
    { label: 'Calls evaluated', value: String(data.callsEvaluated), note: 'All time', icon: Headphones, className: 'bg-sage', iconClass: 'text-sage-foreground' },
    { label: 'Average score', value: data.averageScore === null ? '—' : `${data.averageScore}%`, note: 'Reviewed calls only', icon: Target, className: 'bg-lavender', iconClass: 'text-primary' },
    { label: 'Pending review', value: String(data.pendingReview), note: 'Ready for a manager', icon: Clock, className: 'bg-peach', iconClass: 'text-peach-foreground' },
    { label: 'QA templates', value: String(data.templates), note: 'Saved and reusable', icon: Layers, className: 'bg-sage/50', iconClass: 'text-primary' },
  ]

  return (
    <dl className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {cards.map(({ label, value, note, icon: Icon, className, iconClass }) => (
        <div key={label} className={`flex gap-4 rounded-3xl p-5 ${className}`}>
          <Icon className={`mt-0.5 size-7 shrink-0 ${iconClass}`} aria-hidden="true" />
          <div>
            <dt className="text-sm font-medium text-heading">{label}</dt>
            <dd className="text-4xl font-extrabold text-heading">{value}</dd>
            <dd className="text-xs text-muted-foreground">{note}</dd>
          </div>
        </div>
      ))}
    </dl>
  )
}

function RecentEvaluations({ recent }: { recent: Overview['recent'] }) {
  const navigate = useNavigate()

  return (
    <section className="rounded-3xl border bg-card p-5" aria-labelledby="recent-title">
      <div className="mb-3 flex items-center justify-between">
        <h2 id="recent-title" className="text-xl font-bold">Recent evaluations</h2>
        <Link to="/calls" className="inline-flex items-center gap-1 text-sm font-semibold text-primary hover:underline">
          View all <ArrowRight className="size-4" aria-hidden="true" />
        </Link>
      </div>

      {recent.length === 0 ? (
        <div className="rounded-2xl bg-muted p-6 text-center text-sm text-muted-foreground">
          <p>No calls yet. Evaluate your first call to see it here.</p>
          <Button asChild size="sm" className="mt-3">
            <Link to="/evaluate">Evaluate a call</Link>
          </Button>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[620px] text-left text-sm">
            <caption className="sr-only">The four most recent calls</caption>
            <thead className="bg-lavender/50 text-muted-foreground">
              <tr>
                <th scope="col" className="rounded-l-xl px-3 py-2.5 font-medium">Call</th>
                <th scope="col" className="px-3 py-2.5 font-medium">Template</th>
                <th scope="col" className="px-3 py-2.5 font-medium">Score</th>
                <th scope="col" className="px-3 py-2.5 font-medium">Status</th>
                <th scope="col" className="rounded-r-xl px-3 py-2.5 font-medium">Date</th>
              </tr>
            </thead>
            <tbody>
              {recent.map((call) => (
                <tr
                  key={call.id}
                  onClick={() => navigate(`/calls/${call.id}`)}
                  className="cursor-pointer border-b last:border-b-0 hover:bg-accent/40"
                >
                  <td className="px-3 py-3">
                    <span className="flex items-center gap-3">
                      <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-lavender text-primary">
                        <Play className="size-3.5 fill-current" aria-hidden="true" />
                      </span>
                      <span>
                        <Link
                          to={`/calls/${call.id}`}
                          onClick={(e) => e.stopPropagation()}
                          className="block font-semibold text-heading hover:underline"
                        >
                          {call.title}
                        </Link>
                        <span className="text-xs text-muted-foreground tabular-nums">
                          {call.durationMs ? formatDuration(call.durationMs) : '—'}
                        </span>
                      </span>
                    </span>
                  </td>
                  <td className="px-3 py-3">{call.templateName}</td>
                  <td className="px-3 py-3"><ScorePill value={call.currentScorePercent} /></td>
                  <td className="px-3 py-3"><StatusChip status={call.status} /></td>
                  <td className="px-3 py-3 whitespace-nowrap">{formatDate(call.uploadedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}

function CallToAction() {
  return (
    <section className="relative overflow-hidden rounded-3xl bg-lavender p-6" aria-labelledby="cta-title">
      <div className="pointer-events-none absolute -top-12 -right-12 size-40 rounded-full bg-lavender-strong" aria-hidden="true" />
      <div className="pointer-events-none absolute -bottom-16 left-10 size-40 rounded-full bg-peach" aria-hidden="true" />
      <div className="relative">
        <h2 id="cta-title" className="text-2xl leading-tight font-extrabold">
          Turn conversations into <em className="text-primary">quality insights.</em>
        </h2>
        <p className="mt-3 text-sm text-muted-foreground">
          Upload a call, let AI evaluate it against your QA criteria, and review every result with evidence from the
          recording.
        </p>
        <Button asChild size="lg" className="mt-5">
          <Link to="/evaluate">
            <CloudUpload />
            Evaluate a new call
          </Link>
        </Button>
        <p className="mt-8 text-xs font-bold tracking-widest text-primary uppercase">How it works</p>
        <ol className="mt-2 flex flex-wrap gap-2">
          {['1 · Template', '2 · Upload', '3 · Score', '4 · Review'].map((step) => (
            <li key={step} className="rounded-full bg-card px-3 py-1 text-xs font-semibold text-heading">{step}</li>
          ))}
        </ol>
      </div>
    </section>
  )
}
