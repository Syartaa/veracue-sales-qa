import { useParams } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'

/*
 * /calls/:callId — shows the processing status while the call is being processed,
 * and the Evaluation Review once it's ready (UI spec screens 5, 7 and 8).
 */
export function CallDetailsPage() {
  const { callId } = useParams()

  return (
    <>
      <PageHeader back={{ to: '/calls', label: 'Call History' }} title={`Call ${callId}`} />
      <PlaceholderCard phase={3}>
        <p>Processing status (Phase 3), then the Evaluation Review and Override dialog (Phase 5).</p>
      </PlaceholderCard>
    </>
  )
}
