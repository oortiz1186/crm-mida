using CrmMida.Domain.Common;

namespace CrmMida.Domain.Commercial;

public sealed class Contact : BaseEntity
{
    private Contact() { }

    public Contact(Guid companyId, string firstName, string lastName, string? email)
    {
        CompanyId = companyId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = Normalize(email)?.ToLowerInvariant();
    }

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Position { get; private set; }
    public string? Area { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsPurchasingContact { get; private set; }
    public bool IsTechnicalContact { get; private set; }
    public bool IsBillingContact { get; private set; }
    public bool MarketingConsent { get; private set; }

    public void Update(
        string firstName,
        string lastName,
        string? position,
        string? area,
        string? phone,
        string? mobile,
        string? email,
        bool isPrimary,
        bool isPurchasingContact,
        bool isTechnicalContact,
        bool isBillingContact,
        bool marketingConsent)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Position = Normalize(position);
        Area = Normalize(area);
        Phone = Normalize(phone);
        Mobile = Normalize(mobile);
        Email = Normalize(email)?.ToLowerInvariant();
        IsPrimary = isPrimary;
        IsPurchasingContact = isPurchasingContact;
        IsTechnicalContact = isTechnicalContact;
        IsBillingContact = isBillingContact;
        MarketingConsent = marketingConsent;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPrimary(bool value)
    {
        IsPrimary = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
