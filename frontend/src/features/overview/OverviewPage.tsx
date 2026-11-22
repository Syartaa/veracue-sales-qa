import { CloudUpload } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { Button } from '@/components/ui/button'

function greeting(hour: number) {
  if (hour < 12) return 'Good morning'
  if (hour < 18) return 'Good afternoon'
  return 'Good evening'
}

export function OverviewPage() {
  // Computed once when the page mounts, not on every re-render.
  const [hello] = useState(() => greeting(new Date().getHours()))

  return (
    <>
      <PageHeader
        eyebrow={hello}
        title="Welcome to VeraCue"
        description="Evaluate sales calls, verify results with real evidence, and keep every customer conversation on standard."
        profile="full"
      />
      <div className="space-y-6">
        <PlaceholderCard phase={6}>
          <p>Summary cards (calls evaluated, average score, pending review, QA templates) and recent evaluations.</p>
        </PlaceholderCard>
        <Button asChild size="lg">
          <Link to="/evaluate">
            <CloudUpload />
            Evaluate a new call
          </Link>
        </Button>
      </div>
    </>
  )
}
