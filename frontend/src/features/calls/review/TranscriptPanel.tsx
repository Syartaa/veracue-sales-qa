import { Search } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Input } from '@/components/ui/input'
import { formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { CriterionResult, TranscriptSegment } from '../api'
import { speakerLabel } from './speakers'

type TranscriptPanelProps = {
  transcript: TranscriptSegment[]
  agentSpeaker: number | null
  speakerCount: number
  /** The criterion selected in the scorecard; its evidence segments are highlighted. */
  selected: CriterionResult | null
  /** The segment the audio is currently playing (or was last clicked). */
  activeSegmentId: string | null
  onSegmentClick: (segment: TranscriptSegment) => void
}

export function TranscriptPanel({
  transcript,
  agentSpeaker,
  speakerCount,
  selected,
  activeSegmentId,
  onSegmentClick,
}: TranscriptPanelProps) {
  const [query, setQuery] = useState('')
  const listRef = useRef<HTMLOListElement>(null)

  const evidenceIds = useMemo(() => new Set(selected?.evidence.map((e) => e.segmentId) ?? []), [selected])
  const visible = useMemo(() => {
    const q = query.trim().toLowerCase()
    return q ? transcript.filter((s) => s.text.toLowerCase().includes(q)) : transcript
  }, [query, transcript])

  // When another criterion is selected, scroll its first evidence segment into view (inside the panel only).
  useEffect(() => {
    const first = selected?.evidence[0]
    const list = listRef.current
    if (!first || !list) return
    const row = list.querySelector<HTMLElement>(`[data-segment-id="${first.segmentId}"]`)
    if (row) list.scrollTo({ top: row.offsetTop - list.offsetTop - 16, behavior: 'smooth' })
  }, [selected])

  return (
    <section className="flex flex-col rounded-3xl border bg-card p-5" aria-labelledby="transcript-title">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <h2 id="transcript-title" className="text-lg font-bold">
          Transcript
        </h2>
        <div className="relative w-full sm:w-56">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <Input
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Find in transcript"
            aria-label="Find in transcript"
            className="h-9 pl-9 text-sm"
          />
        </div>
      </div>

      <ol ref={listRef} className="relative -mx-2 space-y-1 overflow-y-auto px-2 lg:max-h-[70vh]">
        {visible.length === 0 && <li className="py-6 text-center text-sm text-muted-foreground">No matching lines.</li>}
        {visible.map((segment) => {
          const isEvidence = evidenceIds.has(segment.id)
          const isActive = segment.id === activeSegmentId
          const label = speakerLabel(segment.speaker, agentSpeaker, speakerCount)
          return (
            <li
              key={segment.id}
              data-segment-id={segment.id}
              className={cn(
                'flex gap-3 rounded-2xl p-2.5 transition-colors',
                isEvidence && 'bg-peach/70',
                isActive && 'ring-2 ring-evidence',
              )}
            >
              <button
                type="button"
                onClick={() => onSegmentClick(segment)}
                className={cn(
                  'h-fit shrink-0 rounded-lg px-2 py-1 text-xs font-bold tabular-nums transition-colors',
                  isEvidence ? 'bg-evidence text-heading' : 'bg-lavender text-primary hover:bg-lavender-strong',
                )}
                aria-label={`Play from ${formatDuration(segment.startMs)}`}
              >
                {formatDuration(segment.startMs)}
              </button>
              <div className="min-w-0">
                <p className="flex flex-wrap items-center gap-2 text-xs font-semibold">
                  <span className={label === 'Customer' ? 'text-sage-foreground' : 'text-primary'}>{label}</span>
                  {isEvidence && selected && (
                    <span className="rounded-full bg-card px-2 py-0.5 text-[11px] font-semibold text-peach-foreground">
                      Evidence · {selected.name}
                    </span>
                  )}
                  {segment.confidence !== null && segment.confidence < 0.6 && (
                    <span className="text-[11px] font-medium text-muted-foreground" title="The transcription model was unsure about this line">
                      low confidence
                    </span>
                  )}
                </p>
                <p className="text-sm text-foreground">{segment.text}</p>
              </div>
            </li>
          )
        })}
      </ol>
    </section>
  )
}
