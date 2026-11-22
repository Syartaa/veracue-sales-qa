import { CircleAlert } from 'lucide-react'

/** Red message under a form field. `id` is referenced by the field's aria-describedby. */
export function FieldError({ id, message }: { id: string; message?: string }) {
  if (!message) return null
  return (
    <p id={id} className="mt-1.5 flex items-center gap-1.5 text-sm font-medium text-danger-foreground">
      <CircleAlert className="size-4 shrink-0" aria-hidden="true" />
      {message}
    </p>
  )
}
