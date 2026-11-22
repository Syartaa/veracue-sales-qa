using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using VeraCue.Api.Services.Transcription;

namespace VeraCue.Api.Services.Evaluation;

/// <summary>
/// Scores a transcript against a template's criteria with a Groq-hosted model.
/// Groq exposes an OpenAI-compatible Chat Completions API; the reply is forced into
/// our JSON Schema with "strict" structured outputs, so it can always be parsed.
/// Docs: https://console.groq.com/docs/structured-outputs
/// </summary>
public sealed class GroqEvaluator(HttpClient http, IConfiguration config)
{
    /// <summary>Stored with every evaluation so results can be traced to the prompt that produced them.</summary>
    public const string PromptVersion = "v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private const string SystemPrompt =
        """
        You are a quality-assurance reviewer for recorded sales calls. Evaluate ONLY the
        transcript provided, against ONLY the criteria provided.

        Rules:
        1. Judge the sales agent's behaviour. First decide which speaker number is the agent
           (the person selling) and return it as agentSpeaker. Something the customer says
           never proves that the agent did something.
        2. Return exactly one result per criterion, using its criterionId.
        3. Outcomes:
           - Met: the agent clearly did what the instructions require.
           - Partial: the agent did only some of what the instructions require.
           - NotMet: the situation required it and the agent did not do it.
           - NotApplicable: the situation the criterion describes never came up in the call.
           - NeedsReview: the transcript is unclear or ambiguous; a human should decide.
        4. score: an integer from 0 to the criterion's maxPoints for Met, Partial and NotMet
           (NotMet is normally 0). Use null for NotApplicable and NeedsReview.
        5. evidenceSegments: the [segment numbers] that directly support your judgment.
           Use only numbers that appear in the transcript; prefer the agent's own lines.
           Met and Partial must cite at least one segment. For NotMet, cite segments that
           show the gap if there are any, otherwise leave it empty. NotApplicable: empty.
        6. explanation: one to three plain sentences about what the agent said or failed to
           say. Do not invent quotes.
        7. Do not use outside knowledge about the company or product.
        """;

    // Strict mode: every property listed in "required", no extra properties allowed.
    private static readonly JsonNode ResponseSchema = JsonNode.Parse(
        """
        {
          "type": "object",
          "properties": {
            "agentSpeaker": { "type": ["integer", "null"] },
            "results": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "criterionId": { "type": "string" },
                  "outcome": { "type": "string", "enum": ["Met", "Partial", "NotMet", "NotApplicable", "NeedsReview"] },
                  "score": { "type": ["integer", "null"] },
                  "explanation": { "type": "string" },
                  "evidenceSegments": { "type": "array", "items": { "type": "integer" } }
                },
                "required": ["criterionId", "outcome", "score", "explanation", "evidenceSegments"],
                "additionalProperties": false
              }
            }
          },
          "required": ["agentSpeaker", "results"],
          "additionalProperties": false
        }
        """)!;

    public async Task<GroqEvaluationResult> EvaluateAsync(
        IReadOnlyList<TranscriptSegment> segments,
        IReadOnlyList<CriterionInput> criteria,
        string? modelOverride,
        CancellationToken cancellationToken)
    {
        var apiKey = config["GROQ_API_KEY"]
            ?? throw new InvalidOperationException("GROQ_API_KEY is not configured.");
        var model = modelOverride ?? config["GROQ_MODEL"] ?? "openai/gpt-oss-120b";

        var requestBody = new JsonObject
        {
            ["model"] = model,
            ["temperature"] = 0,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = BuildUserMessage(segments, criteria) },
            },
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "qa_evaluation",
                    ["strict"] = true,
                    ["schema"] = ResponseSchema.DeepClone(),
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "openai/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ProviderException("Groq", (int)response.StatusCode, body,
                response.Headers.RetryAfter?.Delta);
        }

        var completion = JsonNode.Parse(body)!;
        var content = completion["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Groq returned no message content.");

        var evaluation = JsonSerializer.Deserialize<AiEvaluation>(content, JsonOptions)
            ?? throw new InvalidOperationException("Groq returned an empty evaluation.");

        return new GroqEvaluationResult(
            model,
            PromptVersion,
            evaluation,
            completion["usage"]?["prompt_tokens"]?.GetValue<int>(),
            completion["usage"]?["completion_tokens"]?.GetValue<int>());
    }

    /// <summary>Criteria as JSON, then one transcript line per segment: [index] Speaker N (mm:ss): text</summary>
    private static string BuildUserMessage(
        IReadOnlyList<TranscriptSegment> segments, IReadOnlyList<CriterionInput> criteria)
    {
        var text = new StringBuilder();
        text.AppendLine("Criteria:");
        text.AppendLine(JsonSerializer.Serialize(criteria, JsonOptions));
        text.AppendLine();
        text.AppendLine("Transcript (each line: [segment number] Speaker N (mm:ss): text):");
        foreach (var s in segments)
        {
            var time = TimeSpan.FromMilliseconds(s.StartMs);
            text.AppendLine($"[{s.Index}] Speaker {s.Speaker} ({(int)time.TotalMinutes:00}:{time.Seconds:00}): {s.Text}");
        }
        return text.ToString();
    }
}
