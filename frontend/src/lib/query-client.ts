import { QueryClient } from '@tanstack/react-query'
import { ApiError } from './api'

/**
 * TanStack Query cache shared by the whole app: it stores API responses,
 * tracks loading/error states and refreshes data after changes.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000, // reuse data for 30s before refetching in the background
      retry: (failureCount, error) => {
        // 4xx = our request was wrong (not found, invalid): retrying won't help.
        if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false
        // Network hiccups / 5xx / Render waking up: try twice more.
        return failureCount < 2
      },
    },
  },
})
