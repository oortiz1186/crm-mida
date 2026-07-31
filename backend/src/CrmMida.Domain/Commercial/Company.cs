using CrmMida.Domain.Common;
using CrmMida.Domain.Security;

namespace CrmMida.Domain.Commercial;

public sealed class Company : BaseEntity
{
    private Company() { }

    public Company(
        string tradeName,
        string businessName,
        string rfc,
        string customerType,
        Guid? assignedUserId = null)
    {
        TradeName = tradeName.Trim();
        BusinessName = businessName.Trim();
        Rfc = rfc.Trim().ToUpperInvariant();
        CustomerType = customerType.Trim();
        AssignedUserId = assignedUserId;
    }

    public string TradeName { get; private set; } = string.Empty;
    public string BusinessName { get; private set; } = string.Empty;
    public string Rfc { get; private set; } = string.Empty;
    public string? TaxRegime { get; private set; }
    public string? FiscalPostalCode { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string CustomerType { get; private set; } = string.Empty;
    public string Status { get; private set; } = "active";
    public string? Tags { get; private set; }
    public string? ExternalContpaqiId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public User? AssignedUser { get; private set; }

    public ICollection<Contact> Contacts { get; private set; } = new List<Contact>();

    public void Update(
        string tradeName,
        string businessName,
        string rfc,
        string? taxRegime,
        string? fiscalPostalCode,
        string? email,
        string? phone,
        string? website,
        string? address,
        string? city,
        string? state,
        string customerType,
        string status,
        string? tags,
        string? externalContpaqiId,
        Guid? assignedUserId)
    {
        TradeName = tradeName.Trim();
        BusinessName = businessName.Trim();
        Rfc = rfc.Trim().ToUpperInvariant();
        TaxRegime = Normalize(taxRegime);
        FiscalPostalCode = Normalize(fiscalPostalCode);
        Email = Normalize(email)?.ToLowerInvariant();
        Phone = Normalize(phone);
        Website = Normalize(website);
        Address = Normalize(address);
        City = Normalize(city);
        State = Normalize(state);
        CustomerType = customerType.Trim();
        Status = status.Trim().ToLowerInvariant();
        Tags = Normalize(tags);
        ExternalContpaqiId = Normalize(externalContpaqiId);
        AssignedUserId = assignedUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = "inactive";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
