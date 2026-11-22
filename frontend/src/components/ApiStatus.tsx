import { useEffect, useState } from 'react'
import { checkHealth } from '@/lib/api'
import { cn } from '@/lib/utils'

type Status = 'checking' | 'online' | 'offline'

const labels: Record<Status, string> = {
  checking: 'Checking API…',
  online: 'API connected',
  offline: 'API unreachable',
}

// Temporary development indicator: proves the frontend can call the backend.
export function ApiStatus() {
  const [status, setStatus] = useState<Status>('checking')

  useEffect(() => {
    let cancelled = false
    checkHealth()
      .then(() => !cancelled && setStatus('online'))
      .catch(() => !cancelled && setStatus('offline'))
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <span
      role="status"
      className={cn(
        'inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-semibold',
        status === 'online' && 'bg-sage text-sage-foreground',
        status === 'offline' && 'bg-danger text-danger-foreground',
        status === 'checking' && 'bg-progress text-progress-foreground',
      )}
    >
      <span className="size-2 rounded-full bg-current" aria-hidden="true" />
      {labels[status]}
    </span>
  )
}
