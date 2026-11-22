import { CircleAlert, RotateCw } from 'lucide-react'
import { ApiError } from '@/lib/api'
import { Button } from '@/components/ui/button'

type ErrorStateProps = {
  title?: string
  error: unknown
  onRetry?: () => void
}

/** Friendly error box used when a page can't load its data. */
export function ErrorState({ title = "Couldn't load this page", error, onRetry }: ErrorStateProps) {
  const message =
    error instanceof ApiError && error.status === 0
      ? 'The VeraCue API is not reachable. If the live server was idle, it can take up to a minute to wake up.'
      : error instanceof Error
        ? error.message
        : 'Something went wrong.'

  return (
    <section role="alert" className="flex items-start gap-4 rounded-3xl border border-danger-foreground/20 bg-danger p-6">
      <CircleAlert className="mt-0.5 size-5 shrink-0 text-danger-foreground" aria-hidden="true" />
      <div className="space-y-3">
        <div>
          <p className="font-semibold text-danger-foreground">{title}</p>
          <p className="text-sm text-danger-foreground">{message}</p>
        </div>
        {onRetry && (
          <Button variant="outline" size="sm" onClick={onRetry}>
            <RotateCw />
            Try again
          </Button>
        )}
      </div>
    </section>
  )
}
