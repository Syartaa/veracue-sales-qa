// Shared display formatting, so dates and numbers look the same on every page.

const dateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' })

/** "2026-11-20T09:15:00Z" -> "Nov 20, 2026" (in the viewer's time zone). */
export function formatDate(iso: string) {
  return dateFormatter.format(new Date(iso))
}

/** 1 -> "1 criterion", 3 -> "3 criteria" */
export function plural(count: number, singular: string, pluralForm = `${singular}s`) {
  return `${count} ${count === 1 ? singular : pluralForm}`
}
