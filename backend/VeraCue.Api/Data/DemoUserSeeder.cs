using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Data;

/// <summary>
/// Makes sure the single demo manager account exists (decision: one Users table, one manager).
/// Runs on every startup but only inserts when the account is missing.
/// The password comes from configuration (User Secrets / Render), never from the code.
/// </summary>
public static class DemoUserSeeder
{
    public const string Email = "manager@veracue.demo";
    public const string DisplayName = "Manager";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.AnyAsync(u => u.Email == Email))
            return;

        var password = config["DEMO_MANAGER_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
        {
            // The app still starts; uploads will report that the demo account is missing.
            logger.LogWarning("DEMO_MANAGER_PASSWORD is not set, so the demo manager account was not created.");
            return;
        }

        var user = new User
        {
            Email = Email,
            DisplayName = DisplayName,
            PasswordHash = "", // set just below
            CreatedAt = DateTime.UtcNow,
        };
        // ASP.NET Core's PasswordHasher: salted, slow-by-design hashing (PBKDF2). The plain
        // password is never stored, and the same password gives a different hash each time.
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Created the demo manager account ({Email}).", Email);
    }
}
