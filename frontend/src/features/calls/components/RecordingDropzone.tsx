import { CloudUpload, FileAudio, X } from 'lucide-react'
import { useId, useState, type DragEvent } from 'react'
import { FieldError } from '@/components/FieldError'
import { Button } from '@/components/ui/button'
import { formatBytes, formatDuration } from '@/lib/format'
import { cn } from '@/lib/utils'
import { ACCEPTED_EXTENSIONS } from '../upload-rules'

type RecordingDropzoneProps = {
  file: File | null
  durationMs: number | null
  error?: string
  onSelect: (file: File) => void
  onRemove: () => void
}

/**
 * Drag-and-drop zone that is also a normal file picker: the whole zone is a <label> for a
 * hidden <input type="file">, so clicking, keyboard (Enter/Space) and screen readers all work.
 */
export function RecordingDropzone({ file, durationMs, error, onSelect, onRemove }: RecordingDropzoneProps) {
  const inputId = useId()
  const [dragging, setDragging] = useState(false)

  const handleDrop = (event: DragEvent) => {
    event.preventDefault()
    setDragging(false)
    const dropped = event.dataTransfer.files[0]
    if (dropped) onSelect(dropped)
  }

  return (
    <div>
      <label
        htmlFor={inputId}
        onDragOver={(e) => {
          e.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        className={cn(
          'flex cursor-pointer flex-col items-center justify-center rounded-2xl border-2 border-dashed border-input bg-muted/40 px-6 py-10 text-center transition-colors',
          'hover:border-primary/40 has-focus-visible:ring-[3px] has-focus-visible:ring-ring/50',
          dragging && 'border-primary bg-lavender/60',
          error && 'border-destructive/60',
        )}
      >
        <span className="flex size-12 items-center justify-center rounded-full bg-lavender text-primary">
          <CloudUpload className="size-6" aria-hidden="true" />
        </span>
        <span className="mt-3 font-semibold text-heading">
          Drag a recording here, or <span className="text-primary underline underline-offset-4">browse</span>
        </span>
        <span className="mt-1 text-sm text-muted-foreground">MP3, WAV or M4A · up to 100 MB</span>
        <input
          id={inputId}
          type="file"
          accept={[...ACCEPTED_EXTENSIONS, 'audio/*'].join(',')}
          className="sr-only"
          aria-describedby={error ? 'file-error' : undefined}
          onChange={(e) => {
            const chosen = e.target.files?.[0]
            if (chosen) onSelect(chosen)
            e.target.value = '' // allows choosing the same file again after removing it
          }}
        />
      </label>
      <FieldError id="file-error" message={error} />

      {file && !error && (
        <div className="mt-3 flex items-center gap-3 rounded-2xl border border-sage-foreground/15 bg-sage/70 p-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-card text-sage-foreground">
            <FileAudio className="size-5" aria-hidden="true" />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block truncate font-semibold text-heading">{file.name}</span>
            <span className="block text-sm text-sage-foreground">
              {formatBytes(file.size)}
              {durationMs !== null && ` · ${formatDuration(durationMs)}`} · Ready to upload
            </span>
          </span>
          <Button type="button" variant="ghost" size="icon-sm" onClick={onRemove} aria-label={`Remove ${file.name}`}>
            <X />
          </Button>
        </div>
      )}
    </div>
  )
}
