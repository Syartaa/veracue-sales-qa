import { Layers, Pencil, Search, ShieldCheck, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { formatDate } from '@/lib/format'
import type { TemplateSummary } from '../api'

// Cards rotate through the three brand tints, as in the mockup.
const looks: { icon: LucideIcon; className: string }[] = [
  { icon: Layers, className: 'bg-lavender text-primary' },
  { icon: ShieldCheck, className: 'bg-sage text-sage-foreground' },
  { icon: Search, className: 'bg-peach text-peach-foreground' },
]

const PREVIEW_COUNT = 3

type TemplateCardProps = {
  template: TemplateSummary
  index: number
}

export function TemplateCard({ template, index }: TemplateCardProps) {
  const { icon: Icon, className: tint } = looks[index % looks.length]
  const preview = template.criteria.slice(0, PREVIEW_COUNT)
  const hiddenCount = template.criteria.length - preview.length

  return (
    <article className="flex flex-col rounded-3xl border bg-card p-5 shadow-xs">
      <span className={`flex size-10 items-center justify-center rounded-xl ${tint}`}>
        <Icon className="size-5" aria-hidden="true" />
      </span>

      <h2 className="mt-4 text-lg leading-snug font-bold">{template.name}</h2>
      {template.description && (
        <p className="mt-1 line-clamp-3 text-sm text-muted-foreground">{template.description}</p>
      )}

      <ul className="mt-4 flex flex-wrap gap-1.5" aria-label="Criteria">
        {preview.map((criterion) => (
          <li key={criterion.name} className="rounded-full bg-lavender px-2.5 py-1 text-xs font-medium text-heading">
            {criterion.name} · {criterion.maxPoints}
          </li>
        ))}
        {hiddenCount > 0 && (
          <li className="rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
            +{hiddenCount} more
          </li>
        )}
      </ul>

      {/* mt-auto pushes the footer to the bottom so cards in a row line up */}
      <div className="mt-auto pt-5">
        <p className="flex gap-4 border-t pt-3 text-sm text-muted-foreground">
          <span>
            <strong className="font-bold text-heading">{template.criteriaCount}</strong>{' '}
            {template.criteriaCount === 1 ? 'criterion' : 'criteria'}
          </span>
          <span>
            <strong className="font-bold text-heading">{template.totalPoints}</strong> points
          </span>
        </p>

        <div className="mt-4 grid grid-cols-2 gap-2">
          <Button asChild variant="outline">
            <Link to={`/templates/${template.id}`} aria-label={`Edit ${template.name}`}>
              <Pencil />
              Edit
            </Link>
          </Button>
          <Button asChild variant="secondary">
            <Link to={`/evaluate?templateId=${template.id}`} aria-label={`Use ${template.name}`}>
              Use template
            </Link>
          </Button>
        </div>

        <p className="mt-3 text-xs text-muted-foreground">Updated {formatDate(template.updatedAt)}</p>
      </div>
    </article>
  )
}
