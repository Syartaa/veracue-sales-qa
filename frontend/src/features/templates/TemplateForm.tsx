import { Archive, Check, Plus } from 'lucide-react'
import { useEffect, useMemo, useRef, useState, type SubmitEvent } from 'react'
import { Link, useBlocker, useNavigate } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { FieldError } from '@/components/FieldError'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/lib/api'
import { useArchiveTemplate, useSaveTemplate, type SaveTemplateRequest, type TemplateDetail } from './api'
import { CriterionCard } from './components/CriterionCard'
import { TemplateSidePanel } from './components/TemplateSidePanel'
import {
  LIMITS,
  totalPoints,
  validateTemplate,
  type DraftCriterion,
  type FieldErrors,
  type TemplateDraft,
} from './validation'

// ---------- Draft helpers ----------

function newCriterion(): DraftCriterion {
  return { key: crypto.randomUUID(), id: null, name: '', instructions: '', maxPoints: '10' }
}

function toDraft(template?: TemplateDetail): TemplateDraft {
  if (!template) return { name: '', description: '', criteria: [newCriterion()] }
  return {
    name: template.name,
    description: template.description ?? '',
    criteria: template.criteria.map((c) => ({
      key: c.id,
      id: c.id,
      name: c.name,
      instructions: c.instructions,
      maxPoints: String(c.maxPoints),
    })),
  }
}

function toRequest(draft: TemplateDraft): SaveTemplateRequest {
  return {
    name: draft.name.trim(),
    description: draft.description.trim() || null,
    criteria: draft.criteria.map((c) => ({
      id: c.id,
      name: c.name.trim(),
      instructions: c.instructions.trim(),
      maxPoints: Number(c.maxPoints),
    })),
  }
}

/** Comparable snapshot of the draft, ignoring React keys — used to detect unsaved changes. */
const snapshot = (draft: TemplateDraft) => JSON.stringify(toRequest(draft))

// ---------- Form ----------

type TemplateFormProps = {
  /** Undefined when creating a new template. */
  template?: TemplateDetail
}

export function TemplateForm({ template }: TemplateFormProps) {
  const navigate = useNavigate()
  const isNew = !template
  const saveTemplate = useSaveTemplate(template?.id)
  const archiveTemplate = useArchiveTemplate()

  const [draft, setDraft] = useState(() => toDraft(template))
  const [initialSnapshot] = useState(() => snapshot(toDraft(template)))
  const [touched, setTouched] = useState<Set<string>>(() => new Set())
  const [submitted, setSubmitted] = useState(false)
  const [serverErrors, setServerErrors] = useState<FieldErrors>({})
  const [focusKey, setFocusKey] = useState<string | null>(null)
  const leavingOnPurpose = useRef(false)

  const errors = useMemo(() => validateTemplate(draft), [draft])
  const issueCount = Object.keys(errors).length
  const isDirty = snapshot(draft) !== initialSnapshot

  /** Show an error once the field was left (blur) or after the first save attempt; server errors always. */
  const visibleError = (key: string) =>
    serverErrors[key] ?? (submitted || touched.has(key) ? errors[key] : undefined)

  // ----- Unsaved-changes guard: in-app navigation and closing/reloading the tab -----
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      isDirty && !leavingOnPurpose.current && currentLocation.pathname !== nextLocation.pathname,
  )
  useEffect(() => {
    if (blocker.state !== 'blocked') return
    if (window.confirm('You have unsaved changes. Leave without saving?')) blocker.proceed()
    else blocker.reset()
  }, [blocker])

  useEffect(() => {
    if (!isDirty) return
    const warn = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [isDirty])

  // ----- Editing -----
  const update = (next: TemplateDraft) => {
    setDraft(next)
    setServerErrors({}) // server errors refer to the old content
  }
  const touch = (key: string) => setTouched((prev) => new Set(prev).add(key))

  const updateCriterion = (index: number, field: 'name' | 'instructions' | 'maxPoints', value: string) =>
    update({
      ...draft,
      criteria: draft.criteria.map((c, i) => (i === index ? { ...c, [field]: value } : c)),
    })

  const moveCriterion = (index: number, direction: -1 | 1) => {
    const target = index + direction
    const criteria = [...draft.criteria]
    ;[criteria[index], criteria[target]] = [criteria[target], criteria[index]]
    update({ ...draft, criteria })
    // Touched/error keys are position-based ("criteria[2]…"), so reset them after reordering.
    setTouched(new Set())
  }

  const removeCriterion = (index: number) => {
    update({ ...draft, criteria: draft.criteria.filter((_, i) => i !== index) })
    setTouched(new Set())
  }

  const addCriterion = () => {
    const criterion = newCriterion()
    update({ ...draft, criteria: [...draft.criteria, criterion] })
    setFocusKey(criterion.key)
  }

  // ----- Saving -----
  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitted(true)

    if (issueCount > 0) {
      focusFirstError(errors, draft)
      return
    }

    saveTemplate.mutate(toRequest(draft), {
      onSuccess: (saved) => {
        leavingOnPurpose.current = true
        navigate('/templates', { state: { notice: `“${saved.name}” was saved.` } })
      },
      onError: (error) => {
        // 400/409 from the API: show its messages on the matching fields.
        if (error instanceof ApiError && error.problem?.errors) {
          const fieldErrors = Object.fromEntries(
            Object.entries(error.problem.errors).map(([key, messages]) => [key, messages[0]]),
          )
          setServerErrors(fieldErrors)
          focusFirstError(fieldErrors, draft)
        }
      },
    })
  }

  const handleArchive = () => {
    if (!template) return
    if (!window.confirm(`Archive “${template.name}”? It disappears from the list; past results are kept.`)) return
    archiveTemplate.mutate(template.id, {
      onSuccess: () => {
        leavingOnPurpose.current = true
        navigate('/templates', { state: { notice: `“${template.name}” was archived.` } })
      },
    })
  }

  // A general (non-field) failure, e.g. API unreachable or 500.
  const generalError =
    saveTemplate.error && !(saveTemplate.error instanceof ApiError && saveTemplate.error.problem?.errors)
      ? saveTemplate.error
      : archiveTemplate.error

  return (
    <>
      <PageHeader
        back={{ to: '/templates', label: 'QA Templates' }}
        title={isNew ? 'New template' : 'Edit template'}
        description="Write each criterion as a clear instruction the AI can judge from the transcript."
        actions={
          <>
            <Button asChild variant="outline">
              <Link to="/templates">Cancel</Link>
            </Button>
            <Button type="submit" form="template-form" disabled={saveTemplate.isPending}>
              <Check />
              {saveTemplate.isPending ? 'Saving…' : 'Save template'}
            </Button>
          </>
        }
      />

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px] [&>*]:min-w-0">
        <form id="template-form" onSubmit={handleSubmit} noValidate className="space-y-6">
          {generalError && <ErrorState title="Couldn't save the template" error={generalError} />}

          {/* Template details */}
          <section className="space-y-4 rounded-3xl border bg-card p-5 shadow-xs" aria-labelledby="details-title">
            <h2 id="details-title" className="text-lg font-bold">
              Template details
            </h2>
            <div>
              <Label htmlFor="template-name" className="mb-1.5">
                Template name
              </Label>
              <Input
                id="template-name"
                value={draft.name}
                onChange={(e) => update({ ...draft, name: e.target.value })}
                onBlur={() => touch('name')}
                placeholder="e.g. Life Insurance Sales QA"
                maxLength={LIMITS.nameMax}
                aria-invalid={!!visibleError('name')}
                aria-describedby={visibleError('name') ? 'template-name-error' : undefined}
              />
              <FieldError id="template-name-error" message={visibleError('name')} />
            </div>
            <div>
              <Label htmlFor="template-description" className="mb-1.5">
                Description <span className="font-normal text-muted-foreground">(optional)</span>
              </Label>
              <Textarea
                id="template-description"
                value={draft.description}
                onChange={(e) => update({ ...draft, description: e.target.value })}
                onBlur={() => touch('description')}
                placeholder="What kind of calls is this template for?"
                maxLength={LIMITS.descriptionMax}
                aria-invalid={!!visibleError('description')}
                aria-describedby={visibleError('description') ? 'template-description-error' : undefined}
              />
              <FieldError id="template-description-error" message={visibleError('description')} />
            </div>
          </section>

          {/* Criteria */}
          <section aria-labelledby="criteria-title">
            <div className="mb-3 flex items-baseline justify-between">
              <h2 id="criteria-title" className="text-lg font-bold">
                Criteria <span className="font-medium text-muted-foreground">· {draft.criteria.length}</span>
              </h2>
              <p className="text-sm text-muted-foreground">Use the arrows to reorder</p>
            </div>

            <ol className="space-y-3">
              {draft.criteria.map((criterion, index) => {
                const key = `criteria[${index}]`
                return (
                  <CriterionCard
                    key={criterion.key}
                    criterion={criterion}
                    index={index}
                    count={draft.criteria.length}
                    autoFocus={criterion.key === focusKey}
                    errors={{
                      name: visibleError(`${key}.name`),
                      instructions: visibleError(`${key}.instructions`),
                      maxPoints: visibleError(`${key}.maxPoints`),
                    }}
                    onChange={(field, value) => updateCriterion(index, field, value)}
                    onBlur={(field) => touch(`${key}.${field}`)}
                    onMove={(direction) => moveCriterion(index, direction)}
                    onRemove={() => removeCriterion(index)}
                  />
                )
              })}
            </ol>

            <button
              id="add-criterion"
              type="button"
              onClick={addCriterion}
              disabled={draft.criteria.length >= LIMITS.maxCriteria}
              className="mt-3 flex h-12 w-full items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-input text-sm font-semibold text-primary transition-colors hover:border-primary/40 hover:bg-accent/40 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Plus className="size-4" aria-hidden="true" />
              Add criterion
            </button>
            <FieldError id="criteria-error" message={visibleError('criteria')} />
          </section>

          {!isNew && (
            <div className="flex justify-end border-t pt-4">
              <Button
                type="button"
                variant="ghost"
                onClick={handleArchive}
                disabled={archiveTemplate.isPending}
                className="text-muted-foreground hover:text-destructive"
              >
                <Archive />
                Archive template
              </Button>
            </div>
          )}
        </form>

        <TemplateSidePanel
          criteriaCount={draft.criteria.length}
          totalPoints={totalPoints(draft)}
          issueCount={issueCount}
        />
      </div>
    </>
  )
}

/** Moves keyboard focus to the first field with an error, so it's visible and easy to fix. */
function focusFirstError(errors: FieldErrors, draft: TemplateDraft) {
  const first = Object.keys(errors)[0]
  if (!first) return

  let elementId = 'add-criterion'
  if (first === 'name') elementId = 'template-name'
  else if (first === 'description') elementId = 'template-description'
  else {
    const match = /^criteria\[(\d+)\]\.(name|instructions|maxPoints)$/.exec(first)
    const criterion = match ? draft.criteria[Number(match[1])] : undefined
    if (match && criterion) elementId = `criterion-${criterion.key}-${match[2]}`
  }

  document.getElementById(elementId)?.focus()
}
