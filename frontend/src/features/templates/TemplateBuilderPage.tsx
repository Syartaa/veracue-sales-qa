import { Link, useParams } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api'
import { useTemplate } from './api'
import { TemplateForm } from './TemplateForm'

/**
 * /templates/new  -> empty form
 * /templates/:id  -> loads the template, then shows the same form filled in.
 */
export function TemplateBuilderPage() {
  const { templateId } = useParams()
  const isNew = templateId === 'new'
  const { data: template, isPending, isError, error, refetch } = useTemplate(isNew ? undefined : templateId)

  if (isNew) return <TemplateForm />

  if (isPending) {
    return (
      <>
        <PageHeader back={{ to: '/templates', label: 'QA Templates' }} title="Edit template" />
        <div className="h-96 animate-pulse rounded-3xl border bg-card" aria-busy="true" aria-label="Loading template" />
      </>
    )
  }

  if (isError) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <>
        <PageHeader back={{ to: '/templates', label: 'QA Templates' }} title="Edit template" />
        {notFound ? (
          <section className="rounded-3xl border bg-card p-8 text-center">
            <p className="font-semibold text-heading">This template doesn’t exist or was archived.</p>
            <Button asChild variant="outline" className="mt-4">
              <Link to="/templates">Back to QA Templates</Link>
            </Button>
          </section>
        ) : (
          <ErrorState title="Couldn't load the template" error={error} onRetry={() => refetch()} />
        )}
      </>
    )
  }

  // key: if the URL switches to another template, start a fresh form instead of reusing state.
  return <TemplateForm key={template.id} template={template} />
}
