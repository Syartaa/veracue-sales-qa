using Microsoft.EntityFrameworkCore;
using Npgsql;
using VeraCue.Api.Data;
using VeraCue.Api.DTOs.Templates;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Templates;

/// <summary>
/// Business logic for QA templates: list, read, create, update and archive.
/// Expects requests that already passed <see cref="TemplateValidator"/>.
/// </summary>
public sealed class TemplateService(AppDbContext db, TimeProvider time)
{
    private const string UniqueActiveNameIndex = "IX_QaTemplates_Name_Active";
    private const string NameTakenMessage = "An active template with this name already exists.";

    /// <summary>Active templates, newest change first; optional search in name and description.</summary>
    public async Task<IReadOnlyList<TemplateSummaryDto>> ListAsync(string? search, CancellationToken ct)
    {
        var query = db.QaTemplates.AsNoTracking().Where(t => !t.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE = case-insensitive LIKE. % and _ typed by the user are escaped so they match literally.
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, pattern, @"\") ||
                (t.Description != null && EF.Functions.ILike(t.Description, pattern, @"\")));
        }

        return await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TemplateSummaryDto(
                t.Id,
                t.Name,
                t.Description,
                t.Criteria.Count,
                t.Criteria.Sum(c => c.MaxPoints),
                t.Criteria.OrderBy(c => c.SortOrder).Select(c => new CriterionPreviewDto(c.Name, c.MaxPoints)).ToList(),
                t.UpdatedAt))
            .ToListAsync(ct);
    }

    /// <summary>One active template with its criteria in order, or null if not found / archived.</summary>
    public async Task<TemplateDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var template = await db.QaTemplates.AsNoTracking()
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived, ct);

        return template is null ? null : ToDetail(template);
    }

    /// <summary>Creates the template and all its criteria in one database transaction.</summary>
    public async Task<TemplateDetailDto> CreateAsync(SaveTemplateRequest request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var name = request.Name!.Trim();
        await EnsureNameAvailableAsync(name, excludeId: null, ct);

        var template = new QaTemplate
        {
            Name = name,
            Description = NullIfBlank(request.Description),
            CreatedAt = now,
            UpdatedAt = now,
            Criteria = request.Criteria!
                .Select((c, index) => new QaCriterion
                {
                    Name = c.Name!.Trim(),
                    Instructions = c.Instructions!.Trim(),
                    MaxPoints = c.MaxPoints,
                    SortOrder = index,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
                .ToList(),
        };

        db.QaTemplates.Add(template);
        await SaveAsync(ct); // one SaveChanges = one transaction: template + criteria, or nothing
        return ToDetail(template);
    }

    /// <summary>
    /// Saves edits: updates criteria that kept their id, adds new ones (no id),
    /// deletes ones that were removed, and stores the new order. Null if not found.
    /// </summary>
    public async Task<TemplateDetailDto?> UpdateAsync(Guid id, SaveTemplateRequest request, CancellationToken ct)
    {
        var template = await db.QaTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived, ct);
        if (template is null) return null;

        var now = time.GetUtcNow().UtcDateTime;
        var name = request.Name!.Trim();
        await EnsureNameAvailableAsync(name, excludeId: id, ct);

        var incoming = request.Criteria!;
        var existingById = template.Criteria.ToDictionary(c => c.Id);

        // Every id sent must belong to THIS template.
        for (var i = 0; i < incoming.Count; i++)
        {
            if (incoming[i].Id is { } criterionId && !existingById.ContainsKey(criterionId))
                throw new TemplateRequestException(400, $"criteria[{i}].id", "This criterion doesn't belong to the template.");
        }

        template.Name = name;
        template.Description = NullIfBlank(request.Description);
        template.UpdatedAt = now;

        // Removed criteria: deleted. Past evaluations keep their own snapshot (Phase 4).
        var keptIds = incoming.Where(c => c.Id is not null).Select(c => c.Id!.Value).ToHashSet();
        foreach (var removed in template.Criteria.Where(c => !keptIds.Contains(c.Id)).ToList())
            db.QaCriteria.Remove(removed);

        for (var index = 0; index < incoming.Count; index++)
        {
            var item = incoming[index];
            if (item.Id is { } criterionId)
            {
                var criterion = existingById[criterionId];
                var changed = criterion.Name != item.Name!.Trim()
                    || criterion.Instructions != item.Instructions!.Trim()
                    || criterion.MaxPoints != item.MaxPoints
                    || criterion.SortOrder != index;

                criterion.Name = item.Name!.Trim();
                criterion.Instructions = item.Instructions!.Trim();
                criterion.MaxPoints = item.MaxPoints;
                criterion.SortOrder = index;
                if (changed) criterion.UpdatedAt = now;
            }
            else
            {
                db.QaCriteria.Add(new QaCriterion
                {
                    TemplateId = template.Id,
                    Name = item.Name!.Trim(),
                    Instructions = item.Instructions!.Trim(),
                    MaxPoints = item.MaxPoints,
                    SortOrder = index,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        await SaveAsync(ct);
        return ToDetail(template);
    }

    /// <summary>Archives (soft-deletes) a template. False if not found or already archived.</summary>
    public async Task<bool> ArchiveAsync(Guid id, CancellationToken ct)
    {
        var template = await db.QaTemplates.FirstOrDefaultAsync(t => t.Id == id && !t.IsArchived, ct);
        if (template is null) return false;

        template.IsArchived = true;
        template.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await SaveAsync(ct);
        return true;
    }

    // ---------- helpers ----------

    /// <summary>Friendly check before saving. The unique index is the real guarantee (see SaveAsync).</summary>
    private async Task EnsureNameAvailableAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var lowered = name.ToLower();
        var taken = await db.QaTemplates.AnyAsync(
            t => !t.IsArchived && t.Name.ToLower() == lowered && t.Id != excludeId, ct);

        if (taken) throw new TemplateRequestException(409, "name", NameTakenMessage);
    }

    /// <summary>
    /// Saves, translating a unique-name violation into a friendly 409. This covers the rare
    /// race where two saves with the same name pass the check above at the same moment.
    /// </summary>
    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniqueActiveNameIndex,
        })
        {
            throw new TemplateRequestException(409, "name", NameTakenMessage);
        }
    }

    private static TemplateDetailDto ToDetail(QaTemplate t) => new(
        t.Id,
        t.Name,
        t.Description,
        t.Criteria
            .OrderBy(c => c.SortOrder)
            .Select(c => new CriterionDto(c.Id, c.Name, c.Instructions, c.MaxPoints, c.SortOrder))
            .ToList(),
        t.CreatedAt,
        t.UpdatedAt);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
