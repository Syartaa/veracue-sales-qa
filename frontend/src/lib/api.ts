// Small fetch wrapper for the VeraCue API. Every request goes through apiFetch,
// so base URL, headers and error handling live in one place.

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000').replace(/\/$/, '')

/** Error body the backend returns for every failure (ASP.NET Core ProblemDetails). */
export type ProblemDetails = {
  title?: string
  detail?: string
  status?: number
  /** Field validation errors, e.g. { "Name": ["Name is required."] } */
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  /** HTTP status code, or 0 when the API could not be reached at all. */
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: { Accept: 'application/json', ...init?.headers },
    })
  } catch {
    // Network failure, backend not running, or blocked by CORS.
    throw new ApiError(0, 'Cannot reach the VeraCue API.')
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => undefined)) as ProblemDetails | undefined
    throw new ApiError(
      response.status,
      problem?.detail ?? problem?.title ?? `Request failed (${response.status})`,
      problem,
    )
  }

  if (response.status === 204) return undefined as T

  const contentType = response.headers.get('content-type') ?? ''
  return (contentType.includes('json') ? await response.json() : await response.text()) as T
}

/** GET /api/health — resolves to "Healthy" when the backend is up. */
export function checkHealth() {
  return apiFetch<string>('/api/health', { headers: { Accept: 'text/plain' } })
}
