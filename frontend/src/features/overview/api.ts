import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api'
import type { CallListItem } from '@/features/calls/api'

/** Mirrors backend/VeraCue.Api/DTOs/Calls/CallDtos.cs (OverviewDto). */
export type Overview = {
  callsEvaluated: number
  averageScore: number | null
  pendingReview: number
  templates: number
  recent: CallListItem[]
}

export function useOverview() {
  return useQuery({
    queryKey: ['calls', 'overview'],
    queryFn: () => apiFetch<Overview>('/api/overview'),
  })
}
