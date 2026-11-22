import { UploadCloud } from 'lucide-react'
import { Button } from '@/components/ui/button'

// Temporary theme preview — replaced by the real app layout in the next step.
function App() {
  return (
    <main className="mx-auto max-w-3xl space-y-8 px-4 py-12">
      <header className="space-y-2">
        <p className="text-lg font-medium text-primary">Good morning,</p>
        <h1 className="text-4xl font-extrabold tracking-tight">Welcome to VeraCue</h1>
        <p className="text-muted-foreground">Clarity behind every score.</p>
      </header>

      <section className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-2xl bg-sage p-5 text-sage-foreground">
          <p className="text-sm font-medium">Calls evaluated</p>
          <p className="text-3xl font-bold">24</p>
        </div>
        <div className="rounded-2xl bg-accent p-5 text-accent-foreground">
          <p className="text-sm font-medium">Average score</p>
          <p className="text-3xl font-bold">88%</p>
        </div>
        <div className="rounded-2xl bg-peach p-5 text-peach-foreground">
          <p className="text-sm font-medium">Pending review</p>
          <p className="text-3xl font-bold">5</p>
        </div>
      </section>

      <section className="flex flex-wrap items-center gap-3 rounded-2xl border bg-card p-5">
        <Button size="lg">
          <UploadCloud />
          Evaluate a new call
        </Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="outline">Outline</Button>
        <Button variant="ghost">Ghost</Button>
        <Button variant="destructive">Delete</Button>
      </section>
    </main>
  )
}

export default App
