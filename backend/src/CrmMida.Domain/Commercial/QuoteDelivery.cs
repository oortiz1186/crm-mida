using System.Security.Cryptography;
using System.Text;

namespace CrmMida.Domain.Commercial;

public sealed class QuoteDeliveryAttempt
{
    private QuoteDeliveryAttempt() { }

    public QuoteDeliveryAttempt(Guid quoteId, string channel, string recipient)
    {
        Id = Guid.NewGuid();
        QuoteId = quoteId;
        Channel = NormalizeRequired(channel).ToLowerInvariant();
        Recipient = NormalizeRequired(recipient);
        Status = "pending";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending";
    public string? ProviderReference { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptNumber { get; private set; } = 1;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public void MarkSent(string? providerReference)
    {
        Status = "sent";
        ProviderReference = NormalizeOptional(providerReference);
        ErrorMessage = null;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string message)
    {
        Status = "failed";
        ErrorMessage = NormalizeRequired(message);
        CompletedAtUtc = DateTime.UtcNow;
    }

    public QuoteDeliveryAttempt CreateRetry()
    {
        var retry = new QuoteDeliveryAttempt(QuoteId, Channel, Recipient)
        {
            AttemptNumber = AttemptNumber + 1
        };
        return retry;
    }

    private static string NormalizeRequired(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("El valor es obligatorio.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class QuotePublicAccess
{
    private QuotePublicAccess() { }

    public QuotePublicAccess(Guid quoteId, string token, DateTime expiresAtUtc)
    {
        Id = Guid.NewGuid();
        QuoteId = quoteId;
        TokenHash = HashToken(token);
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? OpenedAtUtc { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }
    public string? Decision { get; private set; }
    public string? DecisionComment { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsValid(string token, DateTime utcNow) =>
        !IsRevoked && RespondedAtUtc is null && ExpiresAtUtc > utcNow && TokenHash == HashToken(token);

    public void RegisterOpen()
    {
        OpenedAtUtc ??= DateTime.UtcNow;
    }

    public void Respond(string decision, string? comment)
    {
        var normalized = decision.Trim().ToLowerInvariant();
        if (normalized is not ("accepted" or "rejected"))
            throw new ArgumentException("La decisión debe ser accepted o rejected.", nameof(decision));

        Decision = normalized;
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        RespondedAtUtc = DateTime.UtcNow;
    }

    public void Revoke() => IsRevoked = true;

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
