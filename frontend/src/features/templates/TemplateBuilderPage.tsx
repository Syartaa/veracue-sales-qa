import { Check } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { PageHeader } from '@/components/layout/PageHeader'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { Button } from '@/components/ui/button'

// One page for both routes: /templates/new (create) and /templates/:templateId (edit).
export function TemplateBuilderPage() {
  const { templateId } = useParams()
  const isNew = templateId === 'new'

  return (
    <>
      <PageHeader
        back={{ to: '/templates', label: 'QA Templates' }}
        title={isNew ? 'New template' : 'Edit template'}
        description="Write each criterion as a clear instruction the AI can judge from the transcript."
        actions={
          <>
            <Button asChild variant="outline">
              <Link to="/templates">Cancel</Link>
            </Button>
            <Button disabled>
              <Check />
              Save template
            </Button>
          </>
        }
      />
      <PlaceholderCard phase={2}>
        <p>Template details, criterion cards (name, max points, instructions), validation and the summary panel.</p>
      </PlaceholderCard>
    </>
  )
}
