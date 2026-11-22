using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using VeraCue.Api.Services;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Transcription;

namespace VeraCue.Api.Dev;

/// <summary>
/// Phase 1b provider test endpoints. Mapped ONLY in the Development environment (see Program.cs),
/// so they never exist on Render. Results are saved to /dev-samples (git-ignored) as fixtures.
/// Removed once the real upload → transcribe → evaluate pipeline exists (Phases 3–4).
/// </summary>
public static class DevEndpoints
{
    private static readonly JsonSerializerOptions FileJson = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public sealed record EvaluateRequest(string TranscriptFile, List<CriterionInput> Criteria, string? Model);

    public static void MapDevEndpoints(this WebApplication app)
    {
        var samplesDir = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "dev-samples"));
        var group = app.MapGroup("/api/dev").AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (ProviderException ex)
            {
                return ProviderProblem(ex);
            }
            catch (InvalidOperationException ex) // e.g. API key not configured (dev-only, safe to show)
            {
                return Results.Problem(ex.Message, statusCode: 500, title: "Configuration error");
            }
        });

        // POST /api/dev/transcribe  (multipart form, field "file")
        group.MapPost("/transcribe", async (IFormFile file, DeepgramTranscriber transcriber, CancellationToken ct) =>
        {
            var stopwatch = Stopwatch.StartNew();
            await using var audio = file.OpenReadStream();
            var result = await transcriber.TranscribeAsync(audio, file.ContentType, ct);
            stopwatch.Stop();

            Directory.CreateDirectory(samplesDir);
            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.transcript.json";
            await File.WriteAllTextAsync(Path.Combine(samplesDir, fileName), JsonSerializer.Serialize(result, FileJson), ct);

            return Results.Ok(new
            {
                savedAs = fileName,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                result.Model,
                result.DurationMs,
                segmentCount = result.Segments.Count,
                speakers = result.Segments.Select(s => s.Speaker).Distinct().Order(),
                result.Segments,
            });
        }).DisableAntiforgery(); // test endpoint called from curl, not a browser form

        // POST /api/dev/evaluate  { transcriptFile, criteria, model? }
        group.MapPost("/evaluate", async (EvaluateRequest request, GroqEvaluator evaluator, CancellationToken ct) =>
        {
            // GetFileName stops "../" tricks: only files inside dev-samples can be read.
            var path = Path.Combine(samplesDir, Path.GetFileName(request.TranscriptFile));
            if (!File.Exists(path))
                return Results.Problem($"Transcript '{request.TranscriptFile}' not found in dev-samples.", statusCode: 404);

            var transcript = JsonSerializer.Deserialize<TranscriptionResult>(await File.ReadAllTextAsync(path, ct), FileJson)!;

            var stopwatch = Stopwatch.StartNew();
            var result = await evaluator.EvaluateAsync(transcript.Segments, request.Criteria, request.Model, ct);
            stopwatch.Stop();

            var issues = AiResultValidator.Validate(result.Evaluation, request.Criteria, transcript.Segments.Count);

            var output = new
            {
                result.Model,
                result.PromptVersion,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                result.PromptTokens,
                result.CompletionTokens,
                validationIssues = issues,
                result.Evaluation.AgentSpeaker,
                result.Evaluation.Results,
            };

            var modelTag = result.Model.Replace('/', '_');
            var outFile = $"{Path.GetFileNameWithoutExtension(path).Replace(".transcript", "")}.evaluation.{modelTag}.json";
            await File.WriteAllTextAsync(Path.Combine(samplesDir, outFile), JsonSerializer.Serialize(output, FileJson), ct);

            return Results.Ok(output);
        });
    }

    /// <summary>Turns provider failures into readable ProblemDetails instead of a generic 500.</summary>
    public static IResult ProviderProblem(ProviderException ex) =>
        Results.Problem(ex.Message, statusCode: ex.IsRateLimited ? 429 : 502, title: $"{ex.Provider} error");
}
