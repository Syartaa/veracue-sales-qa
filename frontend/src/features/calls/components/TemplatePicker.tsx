import { Link } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { FieldError } from '@/components/FieldError'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useTemplates } from '@/features/templates/api'

type TemplatePickerProps = {
  value: string | null
  onChange: (templateId: string) => void
  error?: string
}

/** Card 1: saved templates as selectable radio cards (real radio inputs, so keyboard arrows work). */
export function TemplatePicker({ value, onChange, error }: TemplatePickerProps) {
  const { data: templates, isPending, isError, error: loadError, refetch } = useTemplates('')

  return (
    <fieldset className="rounded-3xl border bg-card p-5 shadow-xs" aria-describedby={error ? 'template-error' : undefined}>
      <legend className="sr-only">QA template</legend>
      <h2 className="mb-4 text-lg font-bold" aria-hidden="true">
        1 · QA template
      </h2>

      {isPending ? (
        <div className="grid gap-3 sm:grid-cols-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-20 animate-pulse rounded-2xl bg-muted" />
          ))}
        </div>
      ) : isError ? (
        <ErrorState title="Couldn't load templates" error={loadError} onRetry={() => refetch()} />
      ) : templates.length === 0 ? (
        <div className="rounded-2xl bg-muted p-5 text-sm text-muted-foreground">
          <p>There are no QA templates yet. Create one first, then come back to evaluate a call.</p>
          <Button asChild variant="outline" size="sm" className="mt-3">
            <Link to="/templates/new">Create a template</Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {templates.map((template) => {
            const selected = template.id === value
            return (
              <label
                key={template.id}
                className={cn(
                  'flex cursor-pointer items-start gap-3 rounded-2xl border bg-card p-4 transition-colors hover:border-primary/40',
                  'has-focus-visible:ring-[3px] has-focus-visible:ring-ring/50',
                  selected && 'border-primary bg-lavender/60 ring-1 ring-primary',
                )}
              >
                <input
                  type="radio"
                  name="template"
                  value={template.id}
                  checked={selected}
                  onChange={() => onChange(template.id)}
                  className="mt-1 size-4 accent-primary"
                />
                <span>
                  <span className="block leading-snug font-semibold text-heading">{template.name}</span>
                  <span className="mt-0.5 block text-sm text-muted-foreground">
                    {template.criteriaCount} {template.criteriaCount === 1 ? 'criterion' : 'criteria'} ·{' '}
                    {template.totalPoints} pts
                  </span>
                </span>
              </label>
            )
          })}
        </div>
      )}
      <FieldError id="template-error" message={error} />
    </fieldset>
  )
}
