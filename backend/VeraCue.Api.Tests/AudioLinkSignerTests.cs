using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using VeraCue.Api.Services.Auth;

namespace VeraCue.Api.Tests;

/// <summary>Signed recording links: valid for one call, until they expire, and not forgeable.</summary>
public class AudioLinkSignerTests
{
    private static AuthSettings Settings(string key = "test-signing-key-that-is-long-enough-123") =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["JWT_SIGNING_KEY"] = key }).Build());

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void A_fresh_link_is_valid_for_its_call()
    {
        var signer = new AudioLinkSigner(Settings(), _clock);
        var callId = Guid.NewGuid();

        var (expires, signature) = signer.Sign(callId);

        Assert.True(signer.IsValid(callId, expires, signature));
    }

    [Fact]
    public void A_link_is_not_valid_for_another_call()
    {
        var signer = new AudioLinkSigner(Settings(), _clock);
        var (expires, signature) = signer.Sign(Guid.NewGuid());

        Assert.False(signer.IsValid(Guid.NewGuid(), expires, signature));
    }

    [Fact]
    public void A_link_stops_working_after_30_minutes()
    {
        var signer = new AudioLinkSigner(Settings(), _clock);
        var callId = Guid.NewGuid();
        var (expires, signature) = signer.Sign(callId);

        _clock.Advance(TimeSpan.FromMinutes(31));

        Assert.False(signer.IsValid(callId, expires, signature));
    }

    [Fact]
    public void Changing_the_expiry_breaks_the_signature()
    {
        var signer = new AudioLinkSigner(Settings(), _clock);
        var callId = Guid.NewGuid();
        var (expires, signature) = signer.Sign(callId);

        Assert.False(signer.IsValid(callId, expires + 86_400, signature)); // "extend" by a day
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("forged-signature")]
    public void Missing_or_forged_signatures_are_rejected(string? signature)
    {
        var signer = new AudioLinkSigner(Settings(), _clock);
        var callId = Guid.NewGuid();
        var (expires, _) = signer.Sign(callId);

        Assert.False(signer.IsValid(callId, expires, signature));
    }

    [Fact]
    public void A_link_signed_with_another_key_is_rejected()
    {
        var callId = Guid.NewGuid();
        var (expires, signature) = new AudioLinkSigner(Settings("another-signing-key-that-is-long-enough-456"), _clock).Sign(callId);

        Assert.False(new AudioLinkSigner(Settings(), _clock).IsValid(callId, expires, signature));
    }

    [Fact]
    public void A_missing_or_short_signing_key_stops_the_app_from_starting()
    {
        Assert.Throws<InvalidOperationException>(() => Settings("too-short"));
    }
}
