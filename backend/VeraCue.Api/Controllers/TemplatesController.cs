using Microsoft.AspNetCore.Mvc;
using VeraCue.Api.DTOs.Templates;
using VeraCue.Api.Services.Templates;

namespace VeraCue.Api.Controllers;

/// <summary>
/// HTTP endpoints for QA templates. Kept thin on purpose: validate the request,
/// call <see cref="TemplateService"/>, and turn the outcome into a status code.
/// </summary>
[ApiController]
[Route("api/templates")]
public class TemplatesController(TemplateService templates) : ControllerBase
{
    /// <summary>GET /api/templates?search=... — active templates for the QA Templates page.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TemplateSummaryDto>>> List(
        [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await templates.ListAsync(search, ct));
    }

    /// <summary>GET /api/templates/{id} — one template for the Template Builder.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var template = await templates.GetAsync(id, ct);
        return template is null ? NotFound() : Ok(template);
    }

    /// <summary>POST /api/templates — create. 201 with the new template, 400 invalid, 409 name taken.</summary>
    [HttpPost]
    public async Task<ActionResult<TemplateDetailDto>> Create(SaveTemplateRequest request, CancellationToken ct)
    {
        var errors = TemplateValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        try
        {
            var created = await templates.CreateAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (TemplateRequestException ex)
        {
            return FieldError(ex);
        }
    }

    /// <summary>PUT /api/templates/{id} — save edits. 200, 400 invalid, 404 not found, 409 name taken.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TemplateDetailDto>> Update(Guid id, SaveTemplateRequest request, CancellationToken ct)
    {
        var errors = TemplateValidator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));

        try
        {
            var updated = await templates.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (TemplateRequestException ex)
        {
            return FieldError(ex);
        }
    }

    /// <summary>DELETE /api/templates/{id} — archives the template (never hard-deletes). 204 or 404.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        return await templates.ArchiveAsync(id, ct) ? NoContent() : NotFound();
    }

    /// <summary>Same JSON shape as validation errors, so the frontend handles both the same way.</summary>
    private ObjectResult FieldError(TemplateRequestException ex)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [ex.Field] = [ex.Message],
        })
        {
            Status = ex.StatusCode,
            Title = ex.StatusCode == 409 ? "Conflict" : "One or more validation errors occurred.",
        };

        return new ObjectResult(problem)
        {
            StatusCode = ex.StatusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
