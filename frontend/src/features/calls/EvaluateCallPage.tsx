import { ArrowRight } from 'lucide-react'
import { useRef, useState, type SubmitEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { ErrorState } from '@/components/ErrorState'
import { FieldError } from '@/components/FieldError'
import { PageHeader } from '@/components/layout/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ApiError } from '@/lib/api'
import { useUploadCall } from './api'
import { RecordingDropzone } from './components/RecordingDropzone'
import { StepIndicator } from './components/StepIndicator'
import { TemplatePicker } from './components/TemplatePicker'
import { WhatHappensNext } from './components/WhatHappensNext'
import { checkFile, readAudioDuration, TITLE_MAX, titleFromFileName } from './upload-rules'

type FieldErrors = Partial<Record<'templateId' | 'file' | 'title' | 'permissionConfirmed', string>>

export function EvaluateCallPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const upload = useUploadCall()

  // "Use template" on a template card links here with ?templateId=…, which preselects it.
  const [templateId, setTemplateId] = useState<string | null>(() => searchParams.get('templateId'))
  const [file, setFile] = useState<File | null>(null)
  const [durationMs, setDurationMs] = useState<number | null>(null)
  const [fileError, setFileError] = useState<string>()
  const [title, setTitle] = useState('')
  const [titleEdited, setTitleEdited] = useState(false)
  const [permission, setPermission] = useState(false)
  const [submitted, setSubmitted] = useState(false)
  const [serverErrors, setServerErrors] = useState<FieldErrors>({})
  const latestFile = useRef<File | null>(null)

  const clientErrors: FieldErrors = {
    templateId: templateId ? undefined : 'Choose a QA template.',
    file: fileError ?? (file ? undefined : 'Choose a call recording to upload.'),
    title: title.trim().length > TITLE_MAX ? `The call title can be at most ${TITLE_MAX} characters.` : undefined,
    permissionConfirmed: permission
      ? undefined
      : "Confirm that you're permitted to process this recording with third-party AI services.",
  }
  const hasErrors = Object.values(clientErrors).some(Boolean)

  /** Field errors appear after the first Start attempt (a bad file is reported immediately). */
  const errorFor = (field: keyof FieldErrors) =>
    serverErrors[field] ?? (field === 'file' && fileError ? fileError : submitted ? clientErrors[field] : undefined)

  const selectFile = async (chosen: File) => {
    setServerErrors({})
    latestFile.current = chosen
    setFile(chosen)
    setDurationMs(null)
    setFileError(checkFile(chosen))
    if (!titleEdited) setTitle(titleFromFileName(chosen.name))

    const duration = await readAudioDuration(chosen)
    if (latestFile.current === chosen) setDurationMs(duration) // ignore if another file was picked meanwhile
  }

  const removeFile = () => {
    latestFile.current = null
    setFile(null)
    setDurationMs(null)
    setFileError(undefined)
    if (!titleEdited) setTitle('')
  }

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitted(true)
    if (hasErrors || !file || !templateId) {
      focusFirstError(clientErrors)
      return
    }

    upload.mutate(
      { file, templateId, title: title.trim(), permissionConfirmed: permission },
      {
        // Stored and queued: go to the Processing page, which follows the call's progress.
        onSuccess: (call) => navigate(`/calls/${call.id}`),
        onError: (error) => {
          if (error instanceof ApiError && error.problem?.errors) {
            const errors = Object.fromEntries(
              Object.entries(error.problem.errors).map(([key, messages]) => [key, messages[0]]),
            ) as FieldErrors
            setServerErrors(errors)
            focusFirstError(errors)
          }
        },
      },
    )
  }

  const generalError =
    upload.error && !(upload.error instanceof ApiError && upload.error.problem?.errors) ? upload.error : null

  return (
    <>
      <PageHeader
        title="Evaluate a call"
        description="Pick a template, add the recording, and VeraCue will transcribe and score it."
      />

      <StepIndicator
        steps={[
          { label: 'Choose template', done: !!templateId },
          { label: 'Add recording', done: !!file && !fileError },
          { label: 'Start evaluation', done: false },
        ]}
      />

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form onSubmit={handleSubmit} noValidate className="space-y-6">
          {generalError && <ErrorState title="Couldn't start the evaluation" error={generalError} />}

          <TemplatePicker
            value={templateId}
            onChange={(id) => {
              setTemplateId(id)
              setServerErrors({})
            }}
            error={errorFor('templateId')}
          />

          <section className="space-y-4 rounded-3xl border bg-card p-5 shadow-xs" aria-labelledby="recording-title">
            <h2 id="recording-title" className="text-lg font-bold">
              2 · Call recording
            </h2>
            <RecordingDropzone
              file={file}
              durationMs={durationMs}
              error={errorFor('file')}
              onSelect={selectFile}
              onRemove={removeFile}
            />
            <div>
              <Label htmlFor="call-title" className="mb-1.5">
                Call title
              </Label>
              <Input
                id="call-title"
                value={title}
                onChange={(e) => {
                  setTitle(e.target.value)
                  setTitleEdited(true)
                }}
                placeholder="e.g. Sales conversation #026 — Hargreaves"
                maxLength={TITLE_MAX}
                aria-invalid={!!errorFor('title')}
                aria-describedby={errorFor('title') ? 'title-error' : undefined}
              />
              <FieldError id="title-error" message={errorFor('title')} />
            </div>
          </section>

          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <label className="flex cursor-pointer items-start gap-3 text-sm text-foreground">
                <input
                  id="permission"
                  type="checkbox"
                  checked={permission}
                  onChange={(e) => setPermission(e.target.checked)}
                  className="mt-0.5 size-5 shrink-0 accent-primary"
                  aria-invalid={!!errorFor('permissionConfirmed')}
                  aria-describedby={errorFor('permissionConfirmed') ? 'permission-error' : undefined}
                />
                I’m permitted to process this recording with third-party AI services.
              </label>
              <FieldError id="permission-error" message={errorFor('permissionConfirmed')} />
            </div>
            <Button type="submit" size="lg" disabled={upload.isPending} className="shrink-0">
              {upload.isPending ? 'Uploading…' : 'Start evaluation'}
              {!upload.isPending && <ArrowRight />}
            </Button>
          </div>
        </form>

        <WhatHappensNext />
      </div>
    </>
  )
}

/** Puts keyboard focus on the first problem so it's visible and quick to fix. */
function focusFirstError(errors: FieldErrors) {
  const order: [keyof FieldErrors, string][] = [
    ['templateId', 'input[name="template"]'],
    ['file', 'input[type="file"]'],
    ['title', '#call-title'],
    ['permissionConfirmed', '#permission'],
  ]
  const first = order.find(([field]) => errors[field])
  if (first) document.querySelector<HTMLElement>(first[1])?.focus()
}
