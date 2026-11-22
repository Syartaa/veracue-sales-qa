import { Plus } from 'lucide-react'
import { Link } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { Button } from '@/components/ui/button'

export function TemplatesPage() {
  return (
    <>
      <PageHeader
        title="QA Templates"
        description="Define what a good call looks like. Each template is a reusable set of criteria with points."
        actions={
          <Button asChild>
            <Link to="/templates/new">
              <Plus />
              New template
            </Link>
          </Button>
        }
      />
      <PlaceholderCard phase={2}>
        <p>Search, template cards (criteria preview, points, calls, Edit / Use template) and "Create a template".</p>
      </PlaceholderCard>
    </>
  )
}
