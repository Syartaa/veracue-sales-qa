import { Lock } from 'lucide-react'

const steps = [
  { title: 'Transcribe', text: 'Speech becomes timestamped segments with speaker labels.' },
  { title: 'Evaluate', text: 'AI scores each criterion and cites the exact segments as evidence.' },
  { title: 'You review', text: 'Check the evidence, override any score, and mark the call reviewed.' },
]

/** Right column of the Evaluate Call page. */
export function WhatHappensNext() {
  return (
    <aside className="space-y-5 lg:sticky lg:top-8">
      <section className="rounded-3xl bg-lavender p-5" aria-labelledby="next-title">
        <h2 id="next-title" className="text-lg font-bold">
          What happens next
        </h2>
        <ol className="mt-4 space-y-4">
          {steps.map((step, i) => (
            <li key={step.title} className="flex gap-3">
              <span className="flex size-8 shrink-0 items-center justify-center rounded-xl bg-card text-sm font-bold text-primary">
                {i + 1}
              </span>
              <span>
                <span className="block font-semibold text-heading">{step.title}</span>
                <span className="block text-sm text-muted-foreground">{step.text}</span>
              </span>
            </li>
          ))}
        </ol>
      </section>

      <section className="flex gap-3 rounded-3xl border bg-card p-5 text-sm text-muted-foreground">
        <Lock className="mt-0.5 size-4 shrink-0 text-sage-foreground" aria-hidden="true" />
        <p>
          Recordings are stored privately. You can leave the next page — processing continues and the result appears
          in Call History.
        </p>
      </section>
    </aside>
  )
}
