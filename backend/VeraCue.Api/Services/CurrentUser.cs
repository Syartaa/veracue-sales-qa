using Microsoft.IdentityModel.JsonWebTokens;

namespace VeraCue.Api.Services;

/// <summary>
/// Who is making this request — used for "uploaded by", "overridden by" and "reviewed by".
/// Read from the signed sign-in token ("sub" claim), which the API has already verified.
/// Every endpoint that uses it requires sign-in, so the id is always present there.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor http)
{
    public Guid Id
    {
        get
        {
            var sub = http.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("No signed-in user for this request.");
        }
    }
}
