// Template rules for instant feedback in the Builder.
// They MIRROR backend/VeraCue.Api/Services/Templates/TemplateValidator.cs — the server
// re-checks everything, so if these ever drift apart, the server's answer wins.

export const LIMITS = {
  nameMax: 150,
  descriptionMax: 1000,
  instructionsMax: 2000,
  minPoints: 1,
  maxPoints: 100,
  maxCriteria: 30,
} as const

/** A criterion while it's being edited. maxPoints is text because it comes straight from an input. */
export type DraftCriterion = {
  key: string // stable React key, also for brand-new criteria that have no id yet
  id: string | null // null = new, not saved yet
  name: string
  instructions: string
  maxPoints: string
}

export type TemplateDraft = {
  name: string
  description: string
  criteria: DraftCriterion[]
}

/** Field errors keyed like the API's: "name", "criteria", "criteria[2].instructions" … */
export type FieldErrors = Record<string, string>

export function validateTemplate(draft: TemplateDraft): FieldErrors {
  const errors: FieldErrors = {}
  const name = draft.name.trim()

  if (!name) errors.name = 'Template name is required.'
  else if (name.length > LIMITS.nameMax) errors.name = `Template name can be at most ${LIMITS.nameMax} characters.`

  if (draft.description.trim().length > LIMITS.descriptionMax)
    errors.description = `Description can be at most ${LIMITS.descriptionMax} characters.`

  if (draft.criteria.length === 0) errors.criteria = 'Add at least one criterion.'
  else if (draft.criteria.length > LIMITS.maxCriteria)
    errors.criteria = `A template can have at most ${LIMITS.maxCriteria} criteria.`

  const seenNames = new Set<string>()
  draft.criteria.forEach((criterion, i) => {
    const key = `criteria[${i}]`
    const criterionName = criterion.name.trim()
    const instructions = criterion.instructions.trim()
    const points = Number(criterion.maxPoints)

    if (!criterionName) errors[`${key}.name`] = 'Criterion name is required.'
    else if (criterionName.length > LIMITS.nameMax)
      errors[`${key}.name`] = `Criterion name can be at most ${LIMITS.nameMax} characters.`
    else if (seenNames.has(criterionName.toLowerCase()))
      errors[`${key}.name`] = 'Another criterion in this template already has this name.'
    else seenNames.add(criterionName.toLowerCase())

    if (!instructions) errors[`${key}.instructions`] = 'Instructions are required so the AI knows what to check.'
    else if (instructions.length > LIMITS.instructionsMax)
      errors[`${key}.instructions`] = `Instructions can be at most ${LIMITS.instructionsMax} characters.`

    if (criterion.maxPoints.trim() === '' || !Number.isInteger(points) || points < LIMITS.minPoints || points > LIMITS.maxPoints)
      errors[`${key}.maxPoints`] = `Max points must be a whole number from ${LIMITS.minPoints} to ${LIMITS.maxPoints}.`
  })

  return errors
}

/** Sum of the points that are valid numbers (for the live "Total points" summary). */
export function totalPoints(draft: TemplateDraft) {
  return draft.criteria.reduce((sum, c) => {
    const points = Number(c.maxPoints)
    return Number.isInteger(points) && points > 0 ? sum + points : sum
  }, 0)
}
