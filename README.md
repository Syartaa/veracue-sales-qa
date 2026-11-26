# VeraCue — Sales Call QA

VeraCue evaluates recorded sales calls against your own QA criteria. Upload a call, get a transcript
and an AI scorecard with evidence from the call, then review and override any score.


**Live app:** https://veracue.vercel.app (demo login provided with the submission)

## Features

- **QA templates:** create, edit, archive and restore templates with any criteria and point values.
- **Evaluate a call:** upload a recording (MP3, WAV or M4A) and pick a template.
- **Transcription:** speaker-labelled transcript with timestamps.
- **AI evaluation:** each criterion is rated Met, Partial, Not met, Not applicable or Needs review,
  with an explanation and links to the supporting part of the call.
- **Manager review:** play the evidence, override scores with a reason, and mark the call as reviewed.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React, TypeScript, Vite, Tailwind CSS, shadcn/ui, TanStack Query (Vercel) |
| Backend | ASP.NET Core 10, EF Core (Docker on Render) |
| Database | PostgreSQL (Neon) |
| AI | Deepgram Nova-3 (transcription), Groq `gpt-oss-120b` (evaluation) |

