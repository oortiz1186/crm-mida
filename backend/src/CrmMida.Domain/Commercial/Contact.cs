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
        Email1 = Email;
    }

    // Se conserva como empresa de origen/legado para no romper los módulos actuales.
    // Las asociaciones reales se administran mediante CompanyContacts.
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Position { get; private set; }
    public string? Area { get; private set; }
    public string? Phone { get; private set; }
    public string? Mobile { get; private set; }
    public string? Email { get; private set; }
    public string? Email1 { get; private set; }
    public string? Email2 { get; private set; }
    public string? Email3 { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsPurchasingContact { get; private set; }
    public bool IsTechnicalContact { get; private set; }
    public bool IsBillingContact { get; private set; }
    public bool MarketingConsent { get; private set; }
    public int? ContpaqiCustomerId { get; private set; }
    public int? ContpaqiAddressId { get; private set; }
    public string? ContpaqiDatabase { get; private set; }
    public DateTime? LastSyncedAtUtc { get; private set; }
    public ICollection<CompanyContact> CompanyContacts { get; private set; } = new List<CompanyContact>();

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
        Email1 = Email;
        IsPrimary = isPrimary;
        IsPurchasingContact = isPurchasingContact;
        IsTechnicalContact = isTechnicalContact;
        IsBillingContact = isBillingContact;
        MarketingConsent = marketingConsent;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateFromContpaqi(
        string fullName,
        string? email1,
        string? email2,
        string? email3,
        string? phone,
        int customerId,
        int? addressId,
        string database)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        FirstName = parts.Length > 0 ? parts[0] : "Contacto";
        LastName = parts.Length > 1 ? parts[1] : string.Empty;
        Position = addressId.HasValue ? "Contacto adicional CONTPAQi" : "Contacto principal CONTPAQi";
        Phone = Normalize(phone);
        Email1 = Normalize(email1);
        Email2 = Normalize(email2);
        Email3 = Normalize(email3);
        Email = Email1;
        ContpaqiCustomerId = customerId;
        ContpaqiAddressId = addressId;
        ContpaqiDatabase = Normalize(database);
        LastSyncedAtUtc = DateTime.UtcNow;
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
