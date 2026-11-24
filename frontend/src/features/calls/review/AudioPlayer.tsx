import { Pause, Play } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { useImperativeHandle, useRef, useState, type Ref } from 'react'
import { apiFetch, apiUrl } from '@/lib/api'
import { formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { CriterionResult } from '../api'

/** What the Review page can tell the player to do. */
export type AudioPlayerHandle = {
  /** Jump to a moment and start playing (used by evidence chips and transcript timestamps). */
  seekTo: (ms: number) => void
}

type AudioPlayerProps = {
  ref?: Ref<AudioPlayerHandle>
  callId: string
  /** Fallback until the browser has read the file's own length. */
  durationMs: number | null
  results: CriterionResult[]
  selectedId: string | null
  /** Called as playback moves, so the transcript can follow along. */
  onTimeChange: (ms: number) => void
}

const SPEEDS = [1, 1.25, 1.5, 2, 0.75]

/**
 * Plays the private recording through the API (GET /api/calls/{id}/audio, which supports
 * Range requests, so jumping to 03:12 doesn't download the whole file first).
 * Evidence markers sit on the timeline: orange for the selected criterion, lilac for the rest.
 */
export function AudioPlayer({ ref, callId, durationMs, results, selectedId, onTimeChange }: AudioPlayerProps) {
  // The recording is private: ask the API (signed in) for a link signed for this call,
  // valid 30 minutes. Reused for 20 minutes, then fetched fresh.
  const link = useQuery({
    queryKey: ['calls', 'audio-url', callId],
    queryFn: () => apiFetch<{ url: string; expiresAt: string }>(`/api/calls/${callId}/audio-url`),
    staleTime: 20 * 60 * 1000,
  })
  const retriedLink = useRef(false)

  const audioRef = useRef<HTMLAudioElement>(null)
  const [playing, setPlaying] = useState(false)
  const [currentMs, setCurrentMs] = useState(0)
  const [totalMs, setTotalMs] = useState(durationMs ?? 0)
  const [speedIndex, setSpeedIndex] = useState(0)
  const [loadError, setLoadError] = useState(false)

  const seekTo = (ms: number) => {
    const audio = audioRef.current
    if (!audio) return
    audio.currentTime = ms / 1000
    setCurrentMs(ms)
    void audio.play().catch(() => {}) // play() can be refused (e.g. autoplay rules); the position still moves
  }
  useImperativeHandle(ref, () => ({ seekTo }))

  const toggle = () => {
    const audio = audioRef.current
    if (!audio) return
    if (audio.paused) void audio.play().catch(() => {})
    else audio.pause()
  }

  const changeSpeed = () => {
    const next = (speedIndex + 1) % SPEEDS.length
    setSpeedIndex(next)
    if (audioRef.current) audioRef.current.playbackRate = SPEEDS[next]
  }

  // Every evidence moment, marked by whether it belongs to the selected criterion.
  const markers = results.flatMap((r) =>
    r.evidence.map((e) => ({ key: `${r.id}-${e.segmentId}`, ms: e.startMs, selected: r.id === selectedId })),
  )

  return (
    <section className="mb-5 rounded-3xl border bg-card p-4 sm:p-5" aria-label="Call recording">
      <audio
        ref={audioRef}
        src={link.data ? apiUrl(link.data.url) : undefined}
        preload="metadata"
        onLoadedMetadata={(e) => Number.isFinite(e.currentTarget.duration) && setTotalMs(e.currentTarget.duration * 1000)}
        onTimeUpdate={(e) => {
          const ms = e.currentTarget.currentTime * 1000
          setCurrentMs(ms)
          onTimeChange(ms)
        }}
        onPlay={() => setPlaying(true)}
        onPause={() => setPlaying(false)}
        onEnded={() => setPlaying(false)}
        onError={() => {
          // Most likely the signed link expired (page open > 30 min): get a fresh one, once.
          if (link.data && !retriedLink.current) {
            retriedLink.current = true
            void link.refetch()
          } else if (link.data) {
            setLoadError(true)
          }
        }}
      />

      <div className="flex items-center gap-3 sm:gap-4">
        <button
          type="button"
          onClick={toggle}
          disabled={loadError}
          aria-label={playing ? 'Pause' : 'Play'}
          className="flex size-12 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50"
        >
          {playing ? <Pause className="size-5 fill-current" /> : <Play className="ml-0.5 size-5 fill-current" />}
        </button>

        <span className="w-12 shrink-0 text-sm font-bold text-heading tabular-nums">{formatDuration(currentMs)}</span>

        <div className="relative min-w-0 flex-1 pt-3">
          {/* Evidence markers (clickable) above the timeline */}
          {totalMs > 0 &&
            markers.map((m) => (
              <button
                key={m.key}
                type="button"
                onClick={() => seekTo(m.ms)}
                tabIndex={-1} /* the same moments are reachable via the evidence chips */
                aria-hidden="true"
                className={cn(
                  'absolute top-0 size-2.5 -translate-x-1/2 rounded-full',
                  m.selected ? 'z-10 bg-evidence ring-2 ring-card' : 'bg-lavender-strong',
                )}
                style={{ left: `${Math.min(100, (m.ms / totalMs) * 100)}%` }}
              />
            ))}
          <input
            type="range"
            min={0}
            max={Math.max(totalMs, 1)}
            step={100}
            value={Math.min(currentMs, totalMs)}
            onChange={(e) => seekTo(Number(e.target.value))}
            disabled={loadError || totalMs === 0}
            aria-label="Position in the recording"
            aria-valuetext={`${formatDuration(currentMs)} of ${formatDuration(totalMs)}`}
            className="mt-1 h-2 w-full cursor-pointer accent-primary"
          />
        </div>

        <span className="hidden w-12 shrink-0 text-sm text-muted-foreground tabular-nums sm:block">{formatDuration(totalMs)}</span>

        <button
          type="button"
          onClick={changeSpeed}
          aria-label={`Playback speed ${SPEEDS[speedIndex]}×, change speed`}
          className="shrink-0 rounded-lg border px-2.5 py-1.5 text-sm font-semibold text-heading hover:bg-accent"
        >
          {SPEEDS[speedIndex]}×
        </button>
      </div>

      <p className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
        {loadError ? (
          <span className="font-medium text-danger-foreground">The recording couldn’t be loaded.</span>
        ) : (
          <>
            <span className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-evidence" /> Evidence for selected criterion
            </span>
            <span className="flex items-center gap-1.5">
              <span className="size-2 rounded-full bg-lavender-strong" /> Other evidence
            </span>
          </>
        )}
      </p>
    </section>
  )
}
