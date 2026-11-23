using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VeraCue.Api.Services.Evaluation;
using VeraCue.Api.Services.Storage;
using VeraCue.Api.Services.Transcription;

namespace VeraCue.Api.Services.Health;

/// <summary>Can the API reach Deepgram with its key? Used by GET /api/health/providers.</summary>
public sealed class DeepgramHealthCheck(DeepgramTranscriber deepgram) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await deepgram.CheckConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("Reachable, API key accepted.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}

/// <summary>Can the API reach Groq with its key? Catches IP blocks (403) from the hosting server.</summary>
public sealed class GroqHealthCheck(GroqEvaluator groq) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await groq.CheckConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("Reachable, API key accepted.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}

/// <summary>Is the recordings disk mounted and writable by the app?</summary>
public sealed class StorageHealthCheck(AudioStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await storage.CheckWritableAsync(cancellationToken);
            return HealthCheckResult.Healthy("Recording storage is writable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>Writes each check's name, status and message as JSON instead of a single word.</summary>
    public static Task WriteJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var body = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = (int)e.Value.Duration.TotalMilliseconds,
            }),
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, Json));
    }
}
