namespace VeraCue.Api.Services;

/// <summary>
/// An external AI provider (Deepgram, Groq) returned an error.
/// Keeps the status code so callers can react, e.g. retry later on 429 (rate limit).
/// </summary>
public sealed class ProviderException(string provider, int statusCode, string responseBody, TimeSpan? retryAfter = null)
    : Exception($"{provider} request failed with HTTP {statusCode}: {Truncate(responseBody)}")
{
    public string Provider { get; } = provider;
    public int StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
    public bool IsRateLimited => StatusCode == 429;

    private static string Truncate(string text) => text.Length <= 500 ? text : text[..500] + "…";
}
