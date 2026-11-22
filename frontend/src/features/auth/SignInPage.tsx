import { Link } from 'react-router'
import { Logo } from '@/components/layout/Logo'
import { PlaceholderCard } from '@/components/PlaceholderCard'
import { Button } from '@/components/ui/button'

// Shown without the sidebar. Real sign-in (demo account) is built in Phase 6.
export function SignInPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center gap-8 px-4 py-12">
      <Logo />
      <PlaceholderCard phase={6}>
        <p>Two-panel sign-in: brand panel on the left, email and password card on the right.</p>
      </PlaceholderCard>
      <Button asChild size="lg" className="self-start">
        <Link to="/">Continue to workspace</Link>
      </Button>
    </main>
  )
}
