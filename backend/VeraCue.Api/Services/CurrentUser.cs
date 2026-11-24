using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;

namespace VeraCue.Api.Services;

/// <summary>
/// Who is doing this request — used for "uploaded by", "overridden by" and "reviewed by".
/// Until sign-in exists (Phase 6) that is always the single demo manager; Phase 6 changes only
/// this class to read the signed-in user instead, and every caller keeps working.
/// </summary>
public sealed class CurrentUser(AppDbContext db)
{
    private Guid? _cachedId;

    /// <summary>The user's id, or null if the demo account hasn't been created (DEMO_MANAGER_PASSWORD missing).</summary>
    public async Task<Guid?> GetIdAsync(CancellationToken ct)
    {
        _cachedId ??= await db.Users
            .Where(u => u.Email == DemoUserSeeder.Email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
        return _cachedId;
    }
}
