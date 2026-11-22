import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/lib/api'

// ---------- Types: mirror the backend DTOs (backend/VeraCue.Api/DTOs/Templates) ----------

export type CriterionPreview = { name: string; maxPoints: number }

export type TemplateSummary = {
  id: string
  name: string
  description: string | null
  criteriaCount: number
  totalPoints: number
  criteria: CriterionPreview[]
  updatedAt: string
}

export type Criterion = {
  id: string
  name: string
  instructions: string
  maxPoints: number
  sortOrder: number
}

export type TemplateDetail = {
  id: string
  name: string
  description: string | null
  criteria: Criterion[]
  createdAt: string
  updatedAt: string
}

/** Body for create and update. List order = saved order; id is null for new criteria. */
export type SaveTemplateRequest = {
  name: string
  description: string | null
  criteria: { id: string | null; name: string; instructions: string; maxPoints: number }[]
}

// ---------- Query keys: one place, so saving can refresh exactly the right data ----------

export const templateKeys = {
  all: ['templates'] as const,
  list: (search: string) => ['templates', 'list', search] as const,
  detail: (id: string) => ['templates', 'detail', id] as const,
}

// ---------- Hooks used by the pages ----------

/** Active templates, optionally filtered. Keeps showing the old list while a new search loads. */
export function useTemplates(search: string) {
  return useQuery({
    queryKey: templateKeys.list(search),
    queryFn: () => apiFetch<TemplateSummary[]>(`/api/templates?search=${encodeURIComponent(search)}`),
    placeholderData: keepPreviousData,
  })
}

export function useTemplate(id: string | undefined) {
  return useQuery({
    queryKey: templateKeys.detail(id ?? ''),
    queryFn: () => apiFetch<TemplateDetail>(`/api/templates/${id}`),
    enabled: !!id,
  })
}

/** Create (no id) or update (with id). On success, every cached template list is refreshed. */
export function useSaveTemplate(id?: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SaveTemplateRequest) =>
      apiFetch<TemplateDetail>(id ? `/api/templates/${id}` : '/api/templates', {
        method: id ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(templateKeys.detail(saved.id), saved)
      return queryClient.invalidateQueries({ queryKey: templateKeys.all })
    },
  })
}

export function useArchiveTemplate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/api/templates/${id}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: templateKeys.all }),
  })
}
