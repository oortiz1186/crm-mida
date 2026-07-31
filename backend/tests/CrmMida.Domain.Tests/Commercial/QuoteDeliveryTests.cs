using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class QuoteDeliveryTests
{
    [Fact]
    public void Delivery_attempt_tracks_success()
    {
        var attempt = new QuoteDeliveryAttempt(Guid.NewGuid(), " Email ", " cliente@mida.mx ");

        attempt.MarkSent("provider-123");

        Assert.Equal("email", attempt.Channel);
        Assert.Equal("cliente@mida.mx", attempt.Recipient);
        Assert.Equal("sent", attempt.Status);
        Assert.Equal("provider-123", attempt.ProviderReference);
        Assert.NotNull(attempt.CompletedAtUtc);
    }

    [Fact]
    public void Retry_increments_attempt_number()
    {
        var attempt = new QuoteDeliveryAttempt(Guid.NewGuid(), "whatsapp", "5214770000000");

        var retry = attempt.CreateRetry();

        Assert.Equal(2, retry.AttemptNumber);
        Assert.Equal(attempt.QuoteId, retry.QuoteId);
    }

    [Fact]
    public void Public_access_validates_token_and_expiration()
    {
        var access = new QuotePublicAccess(Guid.NewGuid(), "secret-token", DateTime.UtcNow.AddDays(1));

        Assert.True(access.IsValid("secret-token", DateTime.UtcNow));
        Assert.False(access.IsValid("another-token", DateTime.UtcNow));
        Assert.False(access.IsValid("secret-token", DateTime.UtcNow.AddDays(2)));
    }

    [Fact]
    public void Public_access_accepts_only_valid_decisions()
    {
        var access = new QuotePublicAccess(Guid.NewGuid(), "secret-token", DateTime.UtcNow.AddDays(1));

        access.Respond("accepted", "De acuerdo");

        Assert.Equal("accepted", access.Decision);
        Assert.Equal("De acuerdo", access.DecisionComment);
        Assert.NotNull(access.RespondedAtUtc);
    }
}
