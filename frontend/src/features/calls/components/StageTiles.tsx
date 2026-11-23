import { Check, LoaderCircle, X } from 'lucide-react'
import { formatTime, plural } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { CallDetail } from '../api'

type TileState = 'done' | 'current' | 'failed' | 'waiting'
type Tile = { title: string; detail: string; state: TileState }

/**
 * Works out the four stage tiles from the call's real status — never an invented percentage.
 * Uploaded → Transcribed → Evaluating → Ready for review
 */
function stagesFor(call: CallDetail): Tile[] {
  const s = call.status
  const transcribed = ['Transcribed', 'Evaluating', 'ReadyForReview', 'Reviewed'].includes(s) ||
    (s === 'Failed' && call.failedStage === 'Evaluation')
  const evaluated = s === 'ReadyForReview' || s === 'Reviewed'

  return [
    { title: 'Uploaded', detail: `Stored privately · ${formatTime(call.uploadedAt)}`, state: 'done' },
    {
      title: 'Transcribed',
      state: transcribed
        ? 'done'
        : s === 'Failed' ? 'failed' : 'current',
      detail: transcribed
        ? `${plural(call.segmentCount, 'segment')} · ${plural(call.speakerCount, 'speaker')}`
        : s === 'Failed' ? 'Transcription stopped' : 'Turning speech into timestamped segments…',
    },
    {
      title: 'Evaluating',
      state: evaluated ? 'done'
        : s === 'Evaluating' ? 'current'
        : s === 'Failed' && call.failedStage === 'Evaluation' ? 'failed'
        : 'waiting',
      detail: evaluated ? 'Criteria scored with evidence'
        : s === 'Evaluating' ? 'Scoring criteria and checking evidence…'
        : s === 'Failed' && call.failedStage === 'Evaluation' ? 'Evaluation stopped'
        : 'Waiting',
    },
    {
      title: 'Ready for review',
      state: evaluated ? 'done' : 'waiting',
      detail: evaluated ? 'Open the review to check each score' : 'Waiting',
    },
  ]
}

export function StageTiles({ call }: { call: CallDetail }) {
  return (
    <ol className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label="Processing stages">
      {stagesFor(call).map((tile, index) => (
        <li
          key={tile.title}
          aria-current={tile.state === 'current' ? 'step' : undefined}
          className={cn(
            'rounded-2xl border p-4',
            tile.state === 'done' && 'border-transparent bg-sage',
            tile.state === 'current' && 'border-primary bg-lavender',
            tile.state === 'failed' && 'border-danger-foreground/30 bg-danger',
            tile.state === 'waiting' && 'border-dashed bg-card',
          )}
        >
          <TileIcon state={tile.state} number={index + 1} />
          <p className="mt-3 font-bold text-heading">{tile.title}</p>
          <p
            className={cn(
              'mt-0.5 text-sm',
              tile.state === 'done' && 'text-sage-foreground',
              tile.state === 'failed' && 'text-danger-foreground',
              (tile.state === 'current' || tile.state === 'waiting') && 'text-muted-foreground',
            )}
          >
            {tile.detail}
          </p>
          <span className="sr-only">
            {tile.state === 'done' ? 'Done' : tile.state === 'current' ? 'In progress' : tile.state === 'failed' ? 'Failed' : 'Not started'}
          </span>
        </li>
      ))}
    </ol>
  )
}

function TileIcon({ state, number }: { state: TileState; number: number }) {
  const base = 'flex size-8 items-center justify-center rounded-full'
  if (state === 'done')
    return <span className={cn(base, 'bg-sage-foreground text-white')}><Check className="size-4" aria-hidden="true" /></span>
  if (state === 'current')
    return <span className={cn(base, 'bg-card text-primary')}><LoaderCircle className="size-5 animate-spin" aria-hidden="true" /></span>
  if (state === 'failed')
    return <span className={cn(base, 'bg-danger-foreground text-white')}><X className="size-4" aria-hidden="true" /></span>
  return <span className={cn(base, 'bg-muted text-sm font-bold text-muted-foreground')}>{number}</span>
}
