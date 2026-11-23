import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

type Step = { label: string; done: boolean }

/** "Choose template ✓ — Add recording ✓ — 3 Start evaluation" progress line. */
export function StepIndicator({ steps }: { steps: Step[] }) {
  const currentIndex = steps.findIndex((s) => !s.done)

  return (
    <ol className="mb-6 flex flex-wrap items-center gap-x-3 gap-y-2" aria-label="Progress">
      {steps.map((step, index) => {
        const isCurrent = index === currentIndex
        return (
          <li key={step.label} className="flex items-center gap-3">
            {index > 0 && <span className="hidden h-0.5 w-8 rounded-full bg-lavender-strong sm:block" aria-hidden="true" />}
            <span className="flex items-center gap-2">
              <span
                className={cn(
                  'flex size-7 items-center justify-center rounded-full text-xs font-bold',
                  step.done && 'bg-sage text-sage-foreground',
                  isCurrent && 'bg-primary text-primary-foreground',
                  !step.done && !isCurrent && 'bg-muted text-muted-foreground',
                )}
              >
                {step.done ? <Check className="size-4" aria-hidden="true" /> : index + 1}
              </span>
              <span
                className={cn(
                  'text-sm font-medium',
                  step.done && 'text-sage-foreground',
                  isCurrent && 'font-semibold text-primary',
                  !step.done && !isCurrent && 'text-muted-foreground',
                )}
              >
                {step.label}
                <span className="sr-only">{step.done ? ' (done)' : isCurrent ? ' (current step)' : ''}</span>
              </span>
            </span>
          </li>
        )
      })}
    </ol>
  )
}
