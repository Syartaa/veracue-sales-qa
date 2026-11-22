import { Link } from 'react-router'
import { PageHeader } from './PageHeader'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  return (
    <>
      <PageHeader title="Page not found" description="This page doesn't exist or has moved." />
      <Button asChild>
        <Link to="/">Back to Overview</Link>
      </Button>
    </>
  )
}
