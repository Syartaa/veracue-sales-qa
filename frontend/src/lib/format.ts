// Shared display formatting, so dates and numbers look the same on every page.

const dateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' })

/** "2026-11-20T09:15:00Z" -> "Nov 20, 2026" (in the viewer's time zone). */
export function formatDate(iso: string) {
  return dateFormatter.format(new Date(iso))
}

/** 8_400_000 -> "8.4 MB" */
export function formatBytes(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/** 522_000 ms -> "08:42"; 3_725_000 ms -> "1:02:05" */
export function formatDuration(ms: number) {
  const totalSeconds = Math.round(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  const mm = String(minutes).padStart(2, '0')
  const ss = String(seconds).padStart(2, '0')
  return hours > 0 ? `${hours}:${mm}:${ss}` : `${mm}:${ss}`
}

/** "2026-11-23T17:42:10Z" -> "17:42" (viewer's time zone) */
export function formatTime(iso: string) {
  return new Intl.DateTimeFormat('en-GB', { hour: '2-digit', minute: '2-digit' }).format(new Date(iso))
}

/** 1 -> "1 criterion", 3 -> "3 criteria" */
export function plural(count: number, singular: string, pluralForm = `${singular}s`) {
  return `${count} ${count === 1 ? singular : pluralForm}`
}
