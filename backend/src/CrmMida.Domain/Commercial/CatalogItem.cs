namespace CrmMida.Domain.Commercial;

public sealed class CatalogItem
{
    private CatalogItem() { }

    public CatalogItem(string code, string name, string type, decimal unitPrice, decimal taxRate = 16m)
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        IsActive = true;
        Update(code, name, type, unitPrice, taxRate, null);
    }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = "service";
    public string? Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(string code, string name, string type, decimal unitPrice, decimal taxRate, string? description)
    {
        Code = Required(code).ToUpperInvariant();
        Name = Required(name);
        Type = NormalizeType(type);
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (taxRate < 0 || taxRate > 100) throw new ArgumentOutOfRangeException(nameof(taxRate));
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        Description = Optional(description);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeType(string value)
    {
        var normalized = Required(value).ToLowerInvariant();
        return normalized is "product" or "service" ? normalized : throw new ArgumentException("El tipo debe ser product o service.", nameof(value));
    }

    private static string Required(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("El valor es obligatorio.");
        return normalized;
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
