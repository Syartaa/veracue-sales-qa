// Upload rules for instant feedback. They MIRROR
// backend/VeraCue.Api/Services/Calls/CallUploadValidator.cs — the server re-checks everything.

export const MAX_FILE_BYTES = 100 * 1024 * 1024
export const ACCEPTED_EXTENSIONS = ['.mp3', '.wav', '.m4a']
export const TITLE_MAX = 200

/** Returns an error message for the chosen file, or undefined if it's fine. */
export function checkFile(file: File): string | undefined {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()
  if (!ACCEPTED_EXTENSIONS.includes(extension)) return 'Upload an MP3, WAV or M4A file.'
  if (file.size === 0) return 'This file is empty.'
  if (file.size > MAX_FILE_BYTES) return 'The recording is larger than 100 MB.'
  return undefined
}

/** "call-2026-11-21-hargreaves.mp3" -> "call-2026-11-21-hargreaves" (default call title). */
export function titleFromFileName(name: string) {
  const dot = name.lastIndexOf('.')
  return dot > 0 ? name.slice(0, dot) : name
}

/**
 * Reads the recording's length in the browser (no upload needed), for the file row.
 * Resolves to null if the browser can't decode the format.
 */
export function readAudioDuration(file: File): Promise<number | null> {
  return new Promise((resolve) => {
    const url = URL.createObjectURL(file)
    const audio = new Audio()
    const done = (value: number | null) => {
      URL.revokeObjectURL(url)
      resolve(value)
    }
    audio.preload = 'metadata'
    audio.onloadedmetadata = () => done(Number.isFinite(audio.duration) ? audio.duration * 1000 : null)
    audio.onerror = () => done(null)
    audio.src = url
  })
}
