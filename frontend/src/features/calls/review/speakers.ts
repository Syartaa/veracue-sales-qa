/**
 * Turns diarization speaker numbers into labels.
 * "Agent" / "Customer" only when the transcript really has two or more speakers AND the AI
 * identified the agent; otherwise neutral "Speaker 1, 2…" — never a confident wrong label
 * (e.g. when both people were detected as one speaker).
 */
export function speakerLabel(speaker: number, agentSpeaker: number | null, speakerCount: number) {
  if (agentSpeaker !== null && speakerCount >= 2) return speaker === agentSpeaker ? 'Agent' : 'Customer'
  return `Speaker ${speaker + 1}`
}
