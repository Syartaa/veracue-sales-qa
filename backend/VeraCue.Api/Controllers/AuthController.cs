using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VeraCue.Api.Data;
using VeraCue.Api.Entities;
using VeraCue.Api.Services;
using VeraCue.Api.Services.Auth;

namespace VeraCue.Api.Controllers;

public sealed record LoginRequest(string? Email, string? Password);
public sealed record UserDto(Guid Id, string Email, string DisplayName);
public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);

/// <summary>Sign-in for the manager workspace (lightweight protection for the public demo).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens, CurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// POST /api/auth/login — checks the password against its stored hash and returns a signed token.
    /// Wrong email and wrong password get the SAME answer, so the response doesn't reveal which accounts exist.
    /// Rate-limited per IP address to slow down password guessing.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        var passwordOk = user is not null
            && !string.IsNullOrEmpty(request.Password)
            && new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

        if (!passwordOk)
        {
            return Problem(
                title: "Sign-in failed",
                detail: "The email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokens.CreateToken(user!);
        return Ok(new LoginResponse(token, expiresAt, new UserDto(user!.Id, user.Email, user.DisplayName)));
    }

    /// <summary>GET /api/auth/me — who the current token belongs to (used when the page reloads).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var id = currentUser.Id;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.Email, u.DisplayName))
            .FirstOrDefaultAsync(ct);
        return user is null ? Unauthorized() : Ok(user);
    }
}
