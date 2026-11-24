# VeraCue — Sales Call QA

VeraCue evaluates recorded sales calls against your own QA criteria. Upload a call, get a transcript
and an AI scorecard with evidence from the call, then review and override any score.

Built for the AMANET Full-Stack Developer take-home assignment.

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

## Running locally

Requires the .NET 10 SDK, Node.js 22, a PostgreSQL database, and Deepgram and Groq API keys.
Keys are stored with .NET User Secrets, never in the repository.

```powershell
# Backend: http://localhost:5000
cd backend
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>" --project VeraCue.Api
dotnet user-secrets set "DEEPGRAM_API_KEY" "<key>" --project VeraCue.Api
dotnet user-secrets set "GROQ_API_KEY" "<key>" --project VeraCue.Api
dotnet user-secrets set "JWT_SIGNING_KEY" "<random string, 32+ characters>" --project VeraCue.Api
dotnet user-secrets set "DEMO_MANAGER_PASSWORD" "<password>" --project VeraCue.Api
dotnet ef database update --project VeraCue.Api
dotnet run --project VeraCue.Api

# Frontend: http://localhost:5173
cd frontend
npm ci
npm run dev
```

Run the tests with `dotnet test` in the `backend` folder.
