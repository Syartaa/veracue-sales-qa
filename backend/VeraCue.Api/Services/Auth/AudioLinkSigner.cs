using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace VeraCue.Api.Services.Auth;

/// <summary>
/// Short-lived signed links for recordings. An &lt;audio&gt; element can't send the sign-in token
/// header, so a signed-in manager first asks for a link; the link itself proves access
/// for ONE call until it expires (the same idea as a pre-signed cloud-storage URL).
/// Signature = HMAC-SHA256(key, "callId:expiresUnixSeconds").
/// </summary>
public sealed class AudioLinkSigner
{
    public static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(30);

    private readonly byte[] _key;
    private readonly TimeProvider _time;

    public AudioLinkSigner(AuthSettings settings, TimeProvider time)
    {
        // A separate key derived for this one purpose, so an audio signature can never be
        // mistaken for (or turned into) anything else signed with the main key.
        _key = HMACSHA256.HashData(settings.KeyBytes, Encoding.UTF8.GetBytes("veracue-audio-links"));
        _time = time;
    }

    public (long Expires, string Signature) Sign(Guid callId)
    {
        var expires = _time.GetUtcNow().Add(LinkLifetime).ToUnixTimeSeconds();
        return (expires, Compute(callId, expires));
    }

    public bool IsValid(Guid callId, long expires, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        if (_time.GetUtcNow().ToUnixTimeSeconds() > expires) return false;

        // Constant-time comparison: doesn't leak how many characters matched.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Compute(callId, expires)),
            Encoding.ASCII.GetBytes(signature));
    }

    private string Compute(Guid callId, long expires) =>
        WebEncoders.Base64UrlEncode(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes($"{callId}:{expires}")));
}
