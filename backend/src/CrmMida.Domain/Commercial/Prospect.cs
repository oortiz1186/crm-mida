using CrmMida.Domain.Common;
using CrmMida.Domain.Security;

namespace CrmMida.Domain.Commercial;

public sealed class Prospect : BaseEntity
{
    private Prospect() { }

    public Prospect(string name, string source, Guid? assignedUserId = null)
    {
        Name = name.Trim();
        Source = source.Trim();
        AssignedUserId = assignedUserId;
    }

    public string Name { get; private set; } = string.Empty;
    public string? CompanyName { get; private set; }
    public string? Rfc { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? Interest { get; private set; }
    public string Status { get; private set; } = "new";
    public string Qualification { get; private set; } = "unrated";
    public string? Notes { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public User? AssignedUser { get; private set; }
    public Guid? ConvertedCompanyId { get; private set; }
    public Company? ConvertedCompany { get; private set; }
    public DateTime? ConvertedAtUtc { get; private set; }

    public void Update(
        string name,
        string? companyName,
        string? rfc,
        string? email,
        string? phone,
        string source,
        string? interest,
        string status,
        string qualification,
        string? notes,
        Guid? assignedUserId)
    {
        Name = name.Trim();
        CompanyName = Normalize(companyName);
        Rfc = Normalize(rfc)?.ToUpperInvariant();
        Email = Normalize(email)?.ToLowerInvariant();
        Phone = Normalize(phone);
        Source = source.Trim();
        Interest = Normalize(interest);
        Status = status.Trim().ToLowerInvariant();
        Qualification = qualification.Trim().ToLowerInvariant();
        Notes = Normalize(notes);
        AssignedUserId = assignedUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkConverted(Guid companyId)
    {
        Status = "converted";
        ConvertedCompanyId = companyId;
        ConvertedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = "discarded";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
