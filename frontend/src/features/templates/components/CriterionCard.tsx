import { ArrowDown, ArrowUp, Trash2 } from 'lucide-react'
import { FieldError } from '@/components/FieldError'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { LIMITS, type DraftCriterion } from '../validation'

type Field = 'name' | 'instructions' | 'maxPoints'

type CriterionCardProps = {
  criterion: DraftCriterion
  index: number
  count: number
  /** Errors to show for this card (already filtered to touched fields / after a save attempt). */
  errors: Partial<Record<Field, string>>
  autoFocus: boolean
  onChange: (field: Field, value: string) => void
  onBlur: (field: Field) => void
  onMove: (direction: -1 | 1) => void
  onRemove: () => void
}

export function CriterionCard({
  criterion,
  index,
  count,
  errors,
  autoFocus,
  onChange,
  onBlur,
  onMove,
  onRemove,
}: CriterionCardProps) {
  const number = index + 1
  const idFor = (field: Field) => `criterion-${criterion.key}-${field}`
  const errorIdFor = (field: Field) => `${idFor(field)}-error`
  const hasErrors = Object.values(errors).some(Boolean)

  return (
    <li
      className={cn(
        'flex gap-3 rounded-3xl border bg-card p-4 shadow-xs sm:gap-4 sm:p-5',
        hasErrors && 'border-destructive/50',
      )}
    >
      {/* Number + reorder buttons (keyboard-friendly alternative to drag-and-drop) */}
      <div className="flex flex-col items-center gap-1">
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => onMove(-1)}
          disabled={index === 0}
          aria-label={`Move criterion ${number} up`}
        >
          <ArrowUp />
        </Button>
        <span className="flex size-7 items-center justify-center rounded-full bg-lavender text-sm font-bold text-primary">
          {number}
        </span>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={() => onMove(1)}
          disabled={index === count - 1}
          aria-label={`Move criterion ${number} down`}
        >
          <ArrowDown />
        </Button>
      </div>

      <div className="min-w-0 flex-1 space-y-3">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
          <div className="min-w-0 flex-1">
            <label htmlFor={idFor('name')} className="sr-only">
              Criterion {number} name
            </label>
            <Input
              id={idFor('name')}
              value={criterion.name}
              onChange={(e) => onChange('name', e.target.value)}
              onBlur={() => onBlur('name')}
              placeholder="Criterion name, e.g. Greeting & introduction"
              maxLength={LIMITS.nameMax}
              autoFocus={autoFocus}
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? errorIdFor('name') : undefined}
              className="font-semibold"
            />
            <FieldError id={errorIdFor('name')} message={errors.name} />
          </div>

          <div className="shrink-0">
            <div className="flex items-center gap-2">
              <label htmlFor={idFor('maxPoints')} className="text-sm whitespace-nowrap text-muted-foreground">
                Max points
              </label>
              <Input
                id={idFor('maxPoints')}
                type="number"
                inputMode="numeric"
                min={LIMITS.minPoints}
                max={LIMITS.maxPoints}
                step={1}
                value={criterion.maxPoints}
                onChange={(e) => onChange('maxPoints', e.target.value)}
                onBlur={() => onBlur('maxPoints')}
                aria-invalid={!!errors.maxPoints}
                aria-describedby={errors.maxPoints ? errorIdFor('maxPoints') : undefined}
                className="w-20 text-center font-semibold"
              />
            </div>
          </div>
        </div>
        <FieldError id={errorIdFor('maxPoints')} message={errors.maxPoints} />

        <div>
          <label htmlFor={idFor('instructions')} className="sr-only">
            Criterion {number} instructions
          </label>
          <Textarea
            id={idFor('instructions')}
            value={criterion.instructions}
            onChange={(e) => onChange('instructions', e.target.value)}
            onBlur={() => onBlur('instructions')}
            placeholder="Describe what the agent must do for this criterion to be met"
            maxLength={LIMITS.instructionsMax}
            aria-invalid={!!errors.instructions}
            aria-describedby={errors.instructions ? errorIdFor('instructions') : undefined}
          />
          <FieldError id={errorIdFor('instructions')} message={errors.instructions} />
        </div>
      </div>

      <div>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={onRemove}
          aria-label={`Delete criterion ${number}`}
          className="text-muted-foreground hover:text-destructive"
        >
          <Trash2 />
        </Button>
      </div>
    </li>
  )
}
