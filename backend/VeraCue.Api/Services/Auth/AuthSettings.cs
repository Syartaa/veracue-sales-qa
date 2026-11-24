using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VeraCue.Api.Services.Auth;

/// <summary>
/// Settings shared by sign-in tokens and signed audio links.
/// JWT_SIGNING_KEY is a secret (User Secrets locally, Render environment in production);
/// anyone with it could create valid tokens, so it never goes into appsettings or Git.
/// </summary>
public sealed class AuthSettings
{
    public const string Issuer = "veracue-api";
    public const string Audience = "veracue-web";
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public SymmetricSecurityKey SigningKey { get; }

    /// <summary>Raw key bytes, also used (with a separate purpose label) to sign audio links.</summary>
    public byte[] KeyBytes { get; }

    public AuthSettings(IConfiguration config)
    {
        var key = config["JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "JWT_SIGNING_KEY is missing or shorter than 32 characters. Set a long random value " +
                "with dotnet user-secrets (locally) or in Render's environment (production).");

        KeyBytes = Encoding.UTF8.GetBytes(key);
        SigningKey = new SymmetricSecurityKey(KeyBytes);
    }
}
