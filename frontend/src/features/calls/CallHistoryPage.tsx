import { CloudUpload } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { ScorePill } from '@/components/ScorePill'
import { type CallStatus, StatusChip } from '@/components/StatusChip'
import { Button } from '@/components/ui/button'

// Temporary preview of the shared chips; removed when the real table is built.
const previewStatuses: CallStatus[] = ['Transcribing', 'Evaluating', 'ReadyForReview', 'Reviewed', 'Failed']

export function CallHistoryPage() {
  return (
    <>
      <PageHeader
        title="Call History"
        description="Every evaluated call, saved. Opening a result never re-runs the AI."
        actions={
          <Button asChild>
            <Link to="/evaluate">
              <CloudUpload />
              Evaluate a call
            </Link>
          </Button>
        }
      />
      <div className="space-y-6">
        <PlaceholderCard phase={5}>
          <p>Search, status and template filters, the calls table and pagination.</p>
          <div className="flex flex-wrap items-center gap-2 pt-2">
            {previewStatuses.map((status) => (
              <StatusChip key={status} status={status} />
            ))}
            <ScorePill value={88} />
            <ScorePill value={null} />
          </div>
        </PlaceholderCard>
      </div>
    </>
  )
}
