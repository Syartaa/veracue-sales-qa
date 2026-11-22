import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'

export function EvaluateCallPage() {
  return (
    <>
      <PageHeader
        title="Evaluate a call"
        description="Pick a template, add the recording, and VeraCue will transcribe and score it."
      />
      <PlaceholderCard phase={3}>
        <p>Template choice, recording drop zone, call title, permission checkbox and Start evaluation.</p>
      </PlaceholderCard>
    </>
  )
}
