import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CallStatus } from '@/components/StatusChip'
import { apiFetch } from '@/lib/api'

// ---------- Types: mirror backend/VeraCue.Api/DTOs/Calls ----------

export type ProcessingStage = 'Transcription' | 'Evaluation'

export type CallDetail = {
  id: string
  title: string
  templateId: string
  templateName: string
  status: CallStatus
  failedStage: ProcessingStage | null
  errorMessage: string | null
  originalFileName: string
  fileSizeBytes: number
  durationMs: number | null
  segmentCount: number
  speakerCount: number
  uploadedAt: string
  updatedAt: string
  aiScorePercent: number | null
  currentScorePercent: number | null
  pendingCount: number
}

// ---------- Review (GET /api/calls/{id}/review) ----------

export type CriterionOutcome = 'Met' | 'Partial' | 'NotMet' | 'NotApplicable' | 'NeedsReview'

export type TranscriptSegment = {
  id: string
  index: number
  speaker: number
  startMs: number
  endMs: number
  text: string
  confidence: number | null
}

export type Evidence = { segmentId: string; segmentIndex: number; startMs: number; speaker: number }

export type CriterionResult = {
  id: string
  sortOrder: number
  name: string
  instructions: string
  maxPoints: number
  aiOutcome: CriterionOutcome
  aiScore: number | null
  aiExplanation: string
  finalOutcome: CriterionOutcome | null
  finalScore: number | null
  overrideReason: string | null
  overriddenByName: string | null
  overriddenAt: string | null
  evidence: Evidence[]
}

export type Score = {
  aiAchieved: number
  aiAvailable: number
  aiPercent: number | null
  currentAchieved: number
  currentAvailable: number
  currentPercent: number | null
  pendingPoints: number
  pendingCount: number
  overrideCount: number
  metCount: number
  partialCount: number
  notMetCount: number
  notApplicableCount: number
  canMarkReviewed: boolean
}

export type CallReview = {
  call: CallDetail
  agentSpeaker: number | null
  transcript: TranscriptSegment[]
  evaluation: {
    id: string
    templateName: string
    modelName: string
    promptVersion: string
    createdAt: string
    reviewedAt: string | null
    reviewedByName: string | null
    score: Score
    results: CriterionResult[]
  } | null
}

export type UploadCallInput = {
  file: File
  templateId: string
  title: string
  permissionConfirmed: boolean
}

/** Statuses where the background worker is still busy, so the page should keep checking. */
export const IN_PROGRESS: CallStatus[] = ['Uploaded', 'Transcribing', 'Transcribed', 'Evaluating']

export const callKeys = {
  all: ['calls'] as const,
  detail: (id: string) => ['calls', 'detail', id] as const,
  review: (id: string) => ['calls', 'review', id] as const,
}

/** Transcript, AI results with evidence and scores. Read-only: never re-runs the AI. */
export function useCallReview(id: string | undefined, enabled = true) {
  return useQuery({
    queryKey: callKeys.review(id ?? ''),
    queryFn: () => apiFetch<CallReview>(`/api/calls/${id}/review`),
    enabled: !!id && enabled,
  })
}

// ---------- Hooks ----------

/** Uploads the recording as multipart/form-data. The API answers 202 as soon as it's stored. */
export function useUploadCall() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ file, templateId, title, permissionConfirmed }: UploadCallInput) => {
      const form = new FormData()
      form.append('file', file)
      form.append('templateId', templateId)
      form.append('title', title)
      form.append('permissionConfirmed', String(permissionConfirmed))
      // No Content-Type header: the browser sets multipart/form-data with the right boundary.
      return apiFetch<CallDetail>('/api/calls', { method: 'POST', body: form })
    },
    onSuccess: (call) => {
      queryClient.setQueryData(callKeys.detail(call.id), call)
      return queryClient.invalidateQueries({ queryKey: callKeys.all })
    },
  })
}

/** One call. While it's being processed, re-checks every 3 seconds; stops when it's done or failed. */
export function useCall(id: string | undefined) {
  return useQuery({
    queryKey: callKeys.detail(id ?? ''),
    queryFn: () => apiFetch<CallDetail>(`/api/calls/${id}`),
    enabled: !!id,
    refetchInterval: (query) => {
      const status = query.state.data?.status
      return status && IN_PROGRESS.includes(status) ? 3000 : false
    },
  })
}

export function useRetryCall(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiFetch<void>(`/api/calls/${id}/retry`, { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: callKeys.detail(id) }),
  })
}
