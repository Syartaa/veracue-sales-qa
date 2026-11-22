using System.Net.Http.Headers;
using System.Text.Json;

namespace VeraCue.Api.Services.Transcription;

/// <summary>
/// Sends a prerecorded audio file to Deepgram and converts the response
/// into our own <see cref="TranscriptSegment"/> list.
/// Docs: https://developers.deepgram.com/docs/pre-recorded-audio
/// </summary>
public sealed class DeepgramTranscriber(HttpClient http, IConfiguration config)
{
    public const string Model = "nova-3";

    // diarize = label speakers; utterances = split into speaker turns with start/end times;
    // smart_format + punctuate = readable text (numbers, punctuation).
    private const string ListenPath =
        $"v1/listen?model={Model}&smart_format=true&punctuate=true&diarize=true&utterances=true";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audio, string contentType, CancellationToken cancellationToken)
    {
        var apiKey = config["DEEPGRAM_API_KEY"]
            ?? throw new InvalidOperationException("DEEPGRAM_API_KEY is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, ListenPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);
        request.Content = new StreamContent(audio);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderException("Deepgram", (int)response.StatusCode, body,
                response.Headers.RetryAfter?.Delta);
        }

        var parsed = JsonSerializer.Deserialize<DeepgramResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Deepgram returned an empty response.");

        // Normalize: milliseconds as integers, trimmed text, empty utterances dropped,
        // then re-numbered so segment indexes are always 0, 1, 2, ... without gaps.
        var segments = (parsed.Results.Utterances ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u.Transcript))
            .Select((u, i) => new TranscriptSegment(
                Index: i,
                Speaker: u.Speaker ?? 0,
                StartMs: ToMs(u.Start),
                EndMs: ToMs(u.End),
                Text: u.Transcript.Trim(),
                Confidence: u.Confidence))
            .ToList();

        return new TranscriptionResult(Model, ToMs(parsed.Metadata.Duration), segments);
    }

    /// <summary>
    /// Cheap connectivity + key check (no audio, no credits used): lists the key's projects.
    /// Throws <see cref="ProviderException"/> if Deepgram can't be reached or rejects the key.
    /// </summary>
    public async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        var apiKey = config["DEEPGRAM_API_KEY"]
            ?? throw new InvalidOperationException("DEEPGRAM_API_KEY is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Get, "v1/projects");
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderException("Deepgram", (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken));
        }
    }

    private static int ToMs(double seconds) => (int)Math.Round(seconds * 1000);

    // Only the parts of Deepgram's response we use.
    private sealed record DeepgramResponse(DeepgramMetadata Metadata, DeepgramResults Results);
    private sealed record DeepgramMetadata(double Duration);
    private sealed record DeepgramResults(List<DeepgramUtterance>? Utterances);
    private sealed record DeepgramUtterance(
        double Start, double End, double? Confidence, int? Speaker, string Transcript);
}
