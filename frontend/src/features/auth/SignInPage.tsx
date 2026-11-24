import { useQueryClient } from '@tanstack/react-query'
import { Lock } from 'lucide-react'
import { useState, type SubmitEvent } from 'react'
import { Navigate, useNavigate, useSearchParams } from 'react-router'
import { FieldError } from '@/components/FieldError'
import { Logo } from '@/components/layout/Logo'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError, login } from '@/lib/api'
import { saveSession, useSession } from '@/lib/session'

const highlights = [
  { title: 'Templates', text: 'Your criteria, your points', className: 'text-primary' },
  { title: 'Evidence', text: 'Every score links to the call', className: 'text-sage-foreground' },
  { title: 'Review', text: 'Managers have the final say', className: 'text-peach-foreground' },
]

/** Only same-app paths, so a crafted ?returnTo=https://evil.example can't redirect elsewhere. */
function safeReturnTo(value: string | null) {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/'
}

/** Sign in (UI spec screen 0): lightweight protection for the public demo. */
export function SignInPage() {
  const session = useSession()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const returnTo = safeReturnTo(searchParams.get('returnTo'))

  const [email, setEmail] = useState('manager@veracue.demo')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string>()
  const [submitting, setSubmitting] = useState(false)

  if (session) return <Navigate to={returnTo} replace />

  const handleSubmit = async (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!email.trim() || !password) {
      setError('Enter your email and password.')
      return
    }

    setSubmitting(true)
    setError(undefined)
    try {
      const newSession = await login(email.trim(), password)
      queryClient.clear() // start clean: nothing cached from before signing in
      saveSession(newSession)
      navigate(returnTo, { replace: true })
    } catch (e) {
      setError(
        e instanceof ApiError && e.status === 429
          ? 'Too many sign-in attempts. Wait a minute and try again.'
          : e instanceof ApiError && e.status === 401
            ? 'The email or password is incorrect.'
            : e instanceof ApiError && e.status === 0
              ? 'The VeraCue API is not reachable. If it was idle, it may need a minute to wake up.'
              : 'Sign-in failed. Try again.',
      )
      setSubmitting(false)
    }
  }

  return (
    <main className="grid min-h-screen lg:grid-cols-2">
      {/* Brand panel */}
      <section className="relative overflow-hidden bg-lavender px-6 py-10 sm:px-12 lg:py-14">
        <div className="pointer-events-none absolute -top-24 right-0 size-72 rounded-full bg-lavender-strong" aria-hidden="true" />
        <div className="pointer-events-none absolute -bottom-24 -left-16 size-72 rounded-full bg-peach" aria-hidden="true" />
        <div className="relative flex h-full flex-col">
          <Logo />
          <div className="my-10 lg:my-auto">
            <h1 className="text-4xl leading-tight font-extrabold tracking-tight sm:text-5xl">
              Clarity behind <em className="text-primary">every score.</em>
            </h1>
            <p className="mt-4 max-w-md text-muted-foreground">
              Score every sales call against your own QA criteria, check each judgment against the exact moment in the
              recording, and correct anything the AI gets wrong.
            </p>
            <ul className="mt-8 grid max-w-lg gap-3 sm:grid-cols-3">
              {highlights.map((h) => (
                <li key={h.title} className="rounded-2xl bg-card p-4">
                  <p className={`text-sm font-bold ${h.className}`}>{h.title}</p>
                  <p className="text-sm text-muted-foreground">{h.text}</p>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>

      {/* Sign-in card */}
      <section className="flex items-center justify-center bg-background px-6 py-10">
        <div className="w-full max-w-md rounded-3xl border bg-card p-8 shadow-xs">
          <h2 className="text-3xl font-extrabold">Sign in</h2>
          <p className="mt-1 text-muted-foreground">Access the manager workspace.</p>

          <form onSubmit={handleSubmit} noValidate className="mt-6 space-y-4">
            <div>
              <Label htmlFor="email" className="mb-1.5">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                aria-invalid={!!error}
              />
            </div>
            <div>
              <Label htmlFor="password" className="mb-1.5">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                autoFocus
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                aria-invalid={!!error}
                aria-describedby={error ? 'signin-error' : undefined}
              />
              <FieldError id="signin-error" message={error} />
            </div>
            <Button type="submit" size="lg" className="w-full" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>

          <p className="mt-6 flex gap-3 rounded-2xl bg-sage px-4 py-3 text-sm text-sage-foreground">
            <Lock className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
            Demo workspace for AMANET reviewers. Recordings stay private and are only reachable after signing in.
          </p>
        </div>
      </section>
    </main>
  )
}
