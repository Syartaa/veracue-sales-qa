using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VeraCue.Api.Entities;

namespace VeraCue.Api.Services.Auth;

/// <summary>
/// Creates the signed sign-in token (JWT) the browser sends with every API request.
/// The signature proves the API issued it; the expiry limits how long a copied token is useful.
/// </summary>
public sealed class TokenService(AuthSettings settings, TimeProvider time)
{
    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(AuthSettings.TokenLifetime);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = AuthSettings.Issuer,
            Audience = AuthSettings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            ]),
            SigningCredentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256),
        });

        return (token, expiresAt);
    }
}
