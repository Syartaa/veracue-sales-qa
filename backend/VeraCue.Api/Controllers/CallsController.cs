using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Services;
using VeraCue.Api.Services.Auth;
using VeraCue.Api.Services.Calls;

namespace VeraCue.Api.Controllers;

public sealed record AudioLinkDto(string Url, DateTime ExpiresAt);

/// <summary>Uploading call recordings, following their processing, and reviewing them.</summary>
[ApiController]
[Route("api/calls")]
public class CallsController(
    CallService calls,
    ReviewService reviews,
    CurrentUser currentUser,
    AudioLinkSigner audioLinks) : ControllerBase
{
    // A little above 100 MB, to leave room for the other form fields around the file.
    private const long MaxRequestBytes = CallUploadValidator.MaxFileBytes + 1024 * 1024;

    /// <summary>
    /// POST /api/calls (multipart/form-data: file, templateId, title, permissionConfirmed).
    /// 202 Accepted: the recording is stored and queued; transcription continues in the background.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<CallDetailDto>> Upload([FromForm] UploadCallRequest request, CancellationToken ct)
    {
        var errors = CallUploadValidator.Validate(
            request.File?.FileName, request.File?.Length ?? 0, request.TemplateId, request.Title, request.PermissionConfirmed);

        if (!errors.ContainsKey("templateId") && !await calls.IsActiveTemplateAsync(request.TemplateId!.Value, ct))
            errors["templateId"] = ["This template doesn't exist or was archived."];

        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        var call = await calls.UploadAsync(request, currentUser.Id, ct);
        return AcceptedAtAction(nameof(Get), new { id = call.Id }, call);
    }

    /// <summary>GET /api/calls?search=&amp;status=&amp;templateId=&amp;page=&amp;pageSize= — Call History.</summary>
    [HttpGet]
    public async Task<ActionResult<CallListDto>> List([FromQuery] CallListQuery query, CancellationToken ct) =>
        Ok(await calls.ListAsync(query, ct));

    /// <summary>
    /// PUT /api/calls/{id}/results/{resultId}/override — a manager's decision for one criterion.
    /// 200 with the updated review, 400 invalid, 404, 409 when locked or changed elsewhere.
    /// </summary>
    [HttpPut("{id:guid}/results/{resultId:guid}/override")]
    public async Task<ActionResult<CallReviewDto>> Override(Guid id, Guid resultId, OverrideRequest request, CancellationToken ct) =>
        ToResponse(await reviews.OverrideAsync(id, resultId, request, currentUser.Id, ct));

    /// <summary>DELETE /api/calls/{id}/results/{resultId}/override?version= — undo an override.</summary>
    [HttpDelete("{id:guid}/results/{resultId:guid}/override")]
    public async Task<ActionResult<CallReviewDto>> ClearOverride(Guid id, Guid resultId, [FromQuery] uint? version, CancellationToken ct) =>
        ToResponse(await reviews.ClearOverrideAsync(id, resultId, version, ct));

    /// <summary>POST /api/calls/{id}/mark-reviewed — only when no criterion is pending; locks the review.</summary>
    [HttpPost("{id:guid}/mark-reviewed")]
    public async Task<ActionResult<CallReviewDto>> MarkReviewed(Guid id, CancellationToken ct) =>
        ToResponse(await reviews.MarkReviewedAsync(id, currentUser.Id, ct));

    private ActionResult<CallReviewDto> ToResponse(ReviewActionResult result) => result.Status switch
    {
        ReviewActionStatus.Ok => Ok(result.Review),
        ReviewActionStatus.NotFound => NotFound(),
        ReviewActionStatus.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors!)),
        _ => Problem(title: "Can't change this review", detail: result.Message, statusCode: StatusCodes.Status409Conflict),
    };

    /// <summary>GET /api/calls/{id} — status and details, polled by the Processing page.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CallDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var call = await calls.GetAsync(id, ct);
        return call is null ? NotFound() : Ok(call);
    }

    /// <summary>
    /// GET /api/calls/{id}/review — transcript, AI results with evidence, and calculated scores.
    /// Read-only: never calls the AI again. "evaluation" is null until the call has been evaluated.
    /// </summary>
    [HttpGet("{id:guid}/review")]
    public async Task<ActionResult<CallReviewDto>> Review(Guid id, CancellationToken ct)
    {
        var review = await reviews.GetReviewAsync(id, ct);
        return review is null ? NotFound() : Ok(review);
    }

    /// <summary>
    /// GET /api/calls/{id}/audio-url — a signed link to this call's recording, valid for 30 minutes.
    /// Requires sign-in; the audio player then uses the link (it can't send the token header).
    /// </summary>
    [HttpGet("{id:guid}/audio-url")]
    public ActionResult<AudioLinkDto> AudioUrl(Guid id)
    {
        var (expires, signature) = audioLinks.Sign(id);
        return Ok(new AudioLinkDto(
            $"/api/calls/{id}/audio?expires={expires}&signature={Uri.EscapeDataString(signature)}",
            DateTimeOffset.FromUnixTimeSeconds(expires).UtcDateTime));
    }

    /// <summary>
    /// GET /api/calls/{id}/audio?expires=&amp;signature= — streams the private recording.
    /// No sign-in header needed, but only with a valid, unexpired signed link from /audio-url.
    /// Range support lets the player jump straight to any timestamp without downloading everything first.
    /// </summary>
    [HttpGet("{id:guid}/audio")]
    [AllowAnonymous]
    public async Task<IActionResult> Audio(Guid id, [FromQuery] long expires, [FromQuery] string? signature, CancellationToken ct)
    {
        if (!audioLinks.IsValid(id, expires, signature)) return Unauthorized();

        var audio = await calls.GetAudioAsync(id, ct);
        if (audio is null) return NotFound();
        return PhysicalFile(audio.Value.FullPath, audio.Value.ContentType, enableRangeProcessing: true);
    }

    /// <summary>POST /api/calls/{id}/retry — restart a failed call at the stage that failed. 202, 404 or 409.</summary>
    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        return await calls.RetryAsync(id, ct) switch
        {
            RetryResult.Retried => Accepted(),
            RetryResult.NotFound => NotFound(),
            _ => Problem(title: "Not failed", detail: "Only a failed call can be retried.", statusCode: StatusCodes.Status409Conflict),
        };
    }
}
