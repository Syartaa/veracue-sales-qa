import { CircleCheck, Plus, Search, X } from 'lucide-react'
import { useState } from 'react'
import { Link, useLocation } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { plural } from '@/lib/format'
import { useDebouncedValue } from '@/lib/use-debounced-value'
import { useTemplates } from './api'
import { CreateTemplateCard } from './components/CreateTemplateCard'
import { TemplateCard } from './components/TemplateCard'

export function TemplatesPage() {
  // Message passed by the Builder after saving or archiving, e.g. "“Sales QA” was saved."
  const notice = (useLocation().state as { notice?: string } | null)?.notice
  const [showNotice, setShowNotice] = useState(true)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim())
  const { data: templates, isPending, isError, error, refetch, isFetching } = useTemplates(debouncedSearch)

  return (
    <>
      <PageHeader
        title="QA Templates"
        description="Define what a good call looks like. Each template is a reusable set of criteria with points."
        actions={
          <Button asChild>
            <Link to="/templates/new">
              <Plus />
              New template
            </Link>
          </Button>
        }
      />

      {notice && showNotice && (
        <div
          role="status"
          className="mb-6 flex items-center justify-between gap-3 rounded-2xl bg-sage px-4 py-3 text-sm font-medium text-sage-foreground"
        >
          <span className="flex items-center gap-2">
            <CircleCheck className="size-4 shrink-0" aria-hidden="true" />
            {notice}
          </span>
          <Button variant="ghost" size="icon-sm" onClick={() => setShowNotice(false)} aria-label="Dismiss message">
            <X />
          </Button>
        </div>
      )}

      <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative w-full sm:max-w-md">
          <Search
            className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search templates"
            aria-label="Search templates"
            className="pl-10"
          />
        </div>
        {templates && (
          <p className="text-sm text-muted-foreground" aria-live="polite">
            {plural(templates.length, 'template')}
            {isFetching && !isPending ? ' · updating…' : ''}
          </p>
        )}
      </div>

      {isPending ? (
        <LoadingGrid />
      ) : isError ? (
        <ErrorState title="Couldn't load templates" error={error} onRetry={() => refetch()} />
      ) : templates.length === 0 && debouncedSearch ? (
        <EmptySearch search={debouncedSearch} onClear={() => setSearch('')} />
      ) : (
        <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
          {templates.map((template, index) => (
            <TemplateCard key={template.id} template={template} index={index} />
          ))}
          {/* With no templates yet, this tile is the empty state and the call to action. */}
          {!debouncedSearch && <CreateTemplateCard />}
        </div>
      )}
    </>
  )
}

function LoadingGrid() {
  return (
    <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-3" aria-busy="true" aria-label="Loading templates">
      {[0, 1, 2].map((i) => (
        <div key={i} className="h-72 animate-pulse rounded-3xl border bg-card" />
      ))}
    </div>
  )
}

function EmptySearch({ search, onClear }: { search: string; onClear: () => void }) {
  return (
    <section className="rounded-3xl border bg-card p-8 text-center">
      <p className="font-semibold text-heading">No templates match “{search}”.</p>
      <p className="mt-1 text-sm text-muted-foreground">Try a different word, or clear the search.</p>
      <Button variant="outline" className="mt-4" onClick={onClear}>
        Clear search
      </Button>
    </section>
  )
}
