import { ChevronRight, CloudUpload, Search } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { ScorePill } from '@/components/ScorePill'
import { StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { formatDate, formatDuration, plural } from '@/lib/format'
import { useDebouncedValue } from '@/lib/use-debounced-value'
import { useTemplates } from '@/features/templates/api'
import { CALL_PAGE_SIZE, useCalls, type CallListQuery } from './api'

const STATUS_OPTIONS: { value: CallListQuery['status']; label: string }[] = [
  { value: '', label: 'All statuses' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'ReadyForReview', label: 'Ready for review' },
  { value: 'Reviewed', label: 'Reviewed' },
  { value: 'Failed', label: 'Failed' },
]

const selectClass =
  'h-11 rounded-lg border border-input bg-card px-3 text-sm text-heading outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50'

/** Call History (UI spec screen 6): reopen any past evaluation — opening never re-runs the AI. */
export function CallHistoryPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<CallListQuery['status']>('')
  const [templateId, setTemplateId] = useState('')
  const [page, setPage] = useState(1)
  const debouncedSearch = useDebouncedValue(search.trim())

  const { data, isPending, isError, error, refetch, isPlaceholderData } = useCalls({
    search: debouncedSearch,
    status,
    templateId,
    page,
  })
  const { data: templates } = useTemplates('')

  const filtered = !!(debouncedSearch || status || templateId)
  const changeFilter = (apply: () => void) => {
    apply()
    setPage(1) // a new filter always starts at the first page
  }

  const from = data && data.total > 0 ? (data.page - 1) * CALL_PAGE_SIZE + 1 : 0
  const to = data ? Math.min(data.page * CALL_PAGE_SIZE, data.total) : 0
  const lastPage = data ? Math.max(1, Math.ceil(data.total / CALL_PAGE_SIZE)) : 1

  return (
    <>
      <PageHeader
        title="Call History"
        description="Every evaluated call, saved. Opening a result never re-runs the AI."
        actions={
          <Button asChild>
            <Link to="/evaluate">
              <CloudUpload />
              Evaluate a call
            </Link>
          </Button>
        }
      />

      <div className="mb-5 flex flex-col gap-3 md:flex-row md:items-center">
        <div className="relative w-full md:max-w-sm">
          <Search className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <Input
            type="search"
            value={search}
            onChange={(e) => changeFilter(() => setSearch(e.target.value))}
            placeholder="Search by call title"
            aria-label="Search by call title"
            className="pl-10"
          />
        </div>
        <label className="flex items-center gap-2 text-sm font-medium text-heading">
          Status
          <select value={status} onChange={(e) => changeFilter(() => setStatus(e.target.value as CallListQuery['status']))} className={selectClass}>
            {STATUS_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
        </label>
        <label className="flex items-center gap-2 text-sm font-medium text-heading">
          Template
          <select value={templateId} onChange={(e) => changeFilter(() => setTemplateId(e.target.value))} className={selectClass}>
            <option value="">All templates</option>
            {templates?.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {isPending ? (
        <div className="h-96 animate-pulse rounded-3xl border bg-card" aria-busy="true" aria-label="Loading calls" />
      ) : isError ? (
        <ErrorState title="Couldn't load the calls" error={error} onRetry={() => refetch()} />
      ) : data.total === 0 ? (
        <section className="rounded-3xl border bg-card p-10 text-center">
          <p className="font-semibold text-heading">{filtered ? 'No calls match these filters.' : 'No calls yet.'}</p>
          <p className="mt-1 text-sm text-muted-foreground">
            {filtered ? 'Try another search or clear the filters.' : 'Upload a recording to evaluate your first call.'}
          </p>
          {filtered ? (
            <Button
              variant="outline"
              className="mt-4"
              onClick={() => changeFilter(() => { setSearch(''); setStatus(''); setTemplateId('') })}
            >
              Clear filters
            </Button>
          ) : (
            <Button asChild className="mt-4">
              <Link to="/evaluate">Evaluate a call</Link>
            </Button>
          )}
        </section>
      ) : (
        <section className={`rounded-3xl border bg-card p-2 transition-opacity sm:p-4 ${isPlaceholderData ? 'opacity-60' : ''}`}>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <caption className="sr-only">Evaluated calls, newest first</caption>
              <thead className="text-muted-foreground">
                <tr>
                  <th scope="col" className="px-3 py-3 font-medium">Call</th>
                  <th scope="col" className="px-3 py-3 font-medium">Template</th>
                  <th scope="col" className="px-3 py-3 font-medium">Duration</th>
                  <th scope="col" className="px-3 py-3 font-medium">Score</th>
                  <th scope="col" className="px-3 py-3 font-medium">Status</th>
                  <th scope="col" className="px-3 py-3 font-medium">Date</th>
                  <th scope="col" className="px-3 py-3"><span className="sr-only">Open</span></th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((call) => (
                  <tr
                    key={call.id}
                    onClick={() => navigate(`/calls/${call.id}`)}
                    className="cursor-pointer border-t transition-colors hover:bg-accent/50"
                  >
                    <td className="px-3 py-3.5 font-semibold text-heading">
                      {/* The real link (keyboard + screen readers); the row click is a mouse shortcut. */}
                      <Link to={`/calls/${call.id}`} onClick={(e) => e.stopPropagation()} className="hover:underline">
                        {call.title}
                      </Link>
                    </td>
                    <td className="px-3 py-3.5">{call.templateName}</td>
                    <td className="px-3 py-3.5 tabular-nums">{call.durationMs ? formatDuration(call.durationMs) : '—'}</td>
                    <td className="px-3 py-3.5">
                      <span className="flex items-center gap-2">
                        <ScorePill value={call.currentScorePercent} />
                        {call.overrideCount > 0 && (
                          <span className="text-xs text-muted-foreground">{plural(call.overrideCount, 'override')}</span>
                        )}
                      </span>
                    </td>
                    <td className="px-3 py-3.5">
                      <StatusChip status={call.status} />
                    </td>
                    <td className="px-3 py-3.5 whitespace-nowrap">{formatDate(call.uploadedAt)}</td>
                    <td className="px-3 py-3.5 text-primary">
                      <ChevronRight className="size-5" aria-hidden="true" />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between gap-3 border-t px-3 pt-4">
            <p className="text-sm text-muted-foreground" aria-live="polite">
              Showing {from}–{to} of {plural(data.total, 'call')}
            </p>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setPage((p) => p - 1)} disabled={page <= 1}>
                Previous
              </Button>
              <Button variant="outline" size="sm" onClick={() => setPage((p) => p + 1)} disabled={page >= lastPage}>
                Next
              </Button>
            </div>
          </div>
        </section>
      )}
    </>
  )
}
