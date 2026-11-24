using Microsoft.AspNetCore.Mvc;
using VeraCue.Api.DTOs.Calls;
using VeraCue.Api.Services;
using VeraCue.Api.Services.Calls;

namespace VeraCue.Api.Controllers;

/// <summary>Uploading call recordings and following their processing.</summary>
[ApiController]
[Route("api/calls")]
public class CallsController(CallService calls, ReviewService reviews, CurrentUser currentUser) : ControllerBase
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

        var uploaderId = await currentUser.GetIdAsync(ct);
        if (uploaderId is null)
        {
            return Problem(
                title: "Demo account missing",
                detail: "The demo manager account hasn't been created. Set DEMO_MANAGER_PASSWORD on the server and restart it.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var call = await calls.UploadAsync(request, uploaderId.Value, ct);
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
    public async Task<ActionResult<CallReviewDto>> Override(Guid id, Guid resultId, OverrideRequest request, CancellationToken ct)
    {
        if (await currentUser.GetIdAsync(ct) is not { } userId) return DemoAccountMissing();
        return ToResponse(await reviews.OverrideAsync(id, resultId, request, userId, ct));
    }

    /// <summary>DELETE /api/calls/{id}/results/{resultId}/override?version= — undo an override.</summary>
    [HttpDelete("{id:guid}/results/{resultId:guid}/override")]
    public async Task<ActionResult<CallReviewDto>> ClearOverride(Guid id, Guid resultId, [FromQuery] uint? version, CancellationToken ct) =>
        ToResponse(await reviews.ClearOverrideAsync(id, resultId, version, ct));

    /// <summary>POST /api/calls/{id}/mark-reviewed — only when no criterion is pending; locks the review.</summary>
    [HttpPost("{id:guid}/mark-reviewed")]
    public async Task<ActionResult<CallReviewDto>> MarkReviewed(Guid id, CancellationToken ct)
    {
        if (await currentUser.GetIdAsync(ct) is not { } userId) return DemoAccountMissing();
        return ToResponse(await reviews.MarkReviewedAsync(id, userId, ct));
    }

    private ActionResult<CallReviewDto> ToResponse(ReviewActionResult result) => result.Status switch
    {
        ReviewActionStatus.Ok => Ok(result.Review),
        ReviewActionStatus.NotFound => NotFound(),
        ReviewActionStatus.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors!)),
        _ => Problem(title: "Can't change this review", detail: result.Message, statusCode: StatusCodes.Status409Conflict),
    };

    private ObjectResult DemoAccountMissing() => Problem(
        title: "Demo account missing",
        detail: "The demo manager account hasn't been created. Set DEMO_MANAGER_PASSWORD on the server and restart it.",
        statusCode: StatusCodes.Status503ServiceUnavailable);

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
    /// GET /api/calls/{id}/audio — streams the private recording to the audio player.
    /// Range support lets the browser jump straight to any timestamp (e.g. an evidence chip)
    /// without downloading the whole file first.
    /// </summary>
    [HttpGet("{id:guid}/audio")]
    public async Task<IActionResult> Audio(Guid id, CancellationToken ct)
    {
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
