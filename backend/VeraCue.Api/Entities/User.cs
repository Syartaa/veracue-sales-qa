namespace VeraCue.Api.Entities;

/// <summary>
/// A manager account (table: Users). There are no roles: every user is a manager.
/// Used to record who uploaded a call and (later) who overrode or reviewed a score.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Stored lower-case so lookups and the unique index are case-insensitive.</summary>
    public required string Email { get; set; }

    /// <summary>Shown next to overrides, e.g. "Manager".</summary>
    public required string DisplayName { get; set; }

    /// <summary>Produced by ASP.NET Core's PasswordHasher — never the plain password.</summary>
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }
}
