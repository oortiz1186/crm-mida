namespace CrmMida.Domain.Commercial;

public sealed class License
{
    private License() { }

    public License(Guid companyId, string productName, string serialNumber, DateTime startsAtUtc, DateTime expiresAtUtc, int users = 1)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("La empresa es obligatoria.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("El producto es obligatorio.", nameof(productName));
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("El número de serie es obligatorio.", nameof(serialNumber));
        if (expiresAtUtc <= startsAtUtc) throw new ArgumentException("La vigencia final debe ser posterior al inicio.", nameof(expiresAtUtc));
        if (users <= 0) throw new ArgumentOutOfRangeException(nameof(users));

        Id = Guid.NewGuid();
        CompanyId = companyId;
        ProductName = productName.Trim();
        SerialNumber = NormalizeSerial(serialNumber);
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Users = users;
        Status = CalculateStatus(DateTime.UtcNow);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string? Version { get; private set; }
    public string? LicenseType { get; private set; }
    public int Users { get; private set; }
    public int Companies { get; private set; } = 1;
    public DateTime StartsAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public string Status { get; private set; } = "active";
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public int DaysToExpire(DateTime utcNow) => (int)Math.Ceiling((ExpiresAtUtc.Date - utcNow.Date).TotalDays);

    public string CalculateStatus(DateTime utcNow)
    {
        Status = ExpiresAtUtc < utcNow ? "expired" : DaysToExpire(utcNow) <= 30 ? "expiring" : "active";
        return Status;
    }

    public void Update(string productName, string? version, string? licenseType, int users, int companies, DateTime startsAtUtc, DateTime expiresAtUtc, string? notes)
    {
        if (string.IsNullOrWhiteSpace(productName)) throw new ArgumentException("El producto es obligatorio.", nameof(productName));
        if (users <= 0 || companies <= 0) throw new ArgumentOutOfRangeException(nameof(users));
        if (expiresAtUtc <= startsAtUtc) throw new ArgumentException("La vigencia final debe ser posterior al inicio.", nameof(expiresAtUtc));

        ProductName = productName.Trim();
        Version = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
        LicenseType = string.IsNullOrWhiteSpace(licenseType) ? null : licenseType.Trim();
        Users = users;
        Companies = companies;
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CalculateStatus(DateTime.UtcNow);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Renew(DateTime newExpiresAtUtc)
    {
        if (newExpiresAtUtc <= ExpiresAtUtc) throw new ArgumentException("La nueva vigencia debe ampliar la licencia.", nameof(newExpiresAtUtc));
        ExpiresAtUtc = newExpiresAtUtc;
        CalculateStatus(DateTime.UtcNow);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeSerial(string value) => value.Trim().ToUpperInvariant().Replace(" ", string.Empty);
}

public sealed class RenewalOpportunity
{
    private RenewalOpportunity() { }

    public RenewalOpportunity(Guid licenseId, DateTime targetDateUtc, decimal estimatedAmount = 0)
    {
        if (licenseId == Guid.Empty) throw new ArgumentException("La licencia es obligatoria.", nameof(licenseId));
        if (estimatedAmount < 0) throw new ArgumentOutOfRangeException(nameof(estimatedAmount));
        Id = Guid.NewGuid();
        LicenseId = licenseId;
        TargetDateUtc = targetDateUtc;
        EstimatedAmount = estimatedAmount;
        Status = "pending";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid LicenseId { get; private set; }
    public License? License { get; private set; }
    public DateTime TargetDateUtc { get; private set; }
    public decimal EstimatedAmount { get; private set; }
    public string Status { get; private set; } = "pending";
    public Guid? OpportunityId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public void LinkOpportunity(Guid opportunityId) => OpportunityId = opportunityId;
    public void Complete(string status, string? notes = null)
    {
        var normalized = status.Trim().ToLowerInvariant();
        if (normalized is not ("renewed" or "lost" or "cancelled")) throw new ArgumentException("Estado de renovación no válido.", nameof(status));
        Status = normalized;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }
}
