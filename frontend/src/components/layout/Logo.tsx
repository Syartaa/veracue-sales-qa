import { AudioLines } from 'lucide-react'
import { Link } from 'react-router'

// VeraCue logo: waveform mark in a rounded square + "CONVERSATION QA" wordmark.
export function Logo() {
  return (
    <Link to="/" className="flex items-center gap-3 rounded-xl" aria-label="VeraCue — go to Overview">
      <span className="flex size-11 items-center justify-center rounded-xl border border-border bg-card text-primary shadow-xs">
        <AudioLines className="size-6" aria-hidden="true" />
      </span>
      <span>
        <span className="block text-xl leading-none font-extrabold text-heading">VeraCue</span>
        <span className="mt-1 block text-[10px] font-semibold tracking-[0.18em] text-muted-foreground">
          CONVERSATION QA
        </span>
      </span>
    </Link>
  )
}
