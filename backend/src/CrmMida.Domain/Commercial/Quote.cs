namespace CrmMida.Domain.Commercial;

public sealed class Quote
{
    private readonly List<QuoteItem> _items = [];

    private Quote() { }

    public Quote(Guid companyId, string title, DateTime validUntilUtc, Guid? opportunityId = null, Guid? contactId = null)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Title = NormalizeRequired(title, nameof(title));
        ValidUntilUtc = validUntilUtc;
        OpportunityId = opportunityId;
        ContactId = contactId;
        Status = "draft";
        Currency = "MXN";
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public string Folio { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public Guid? ContactId { get; private set; }
    public Contact? Contact { get; private set; }
    public Guid? OpportunityId { get; private set; }
    public Opportunity? Opportunity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "MXN";
    public decimal Discount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
    public DateTime ValidUntilUtc { get; private set; }
    public string Status { get; private set; } = "draft";
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<QuoteItem> Items => _items;

    public void SetFolio(string folio)
    {
        Folio = NormalizeRequired(folio, nameof(folio)).ToUpperInvariant();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string title, DateTime validUntilUtc, string currency, decimal discount, string? notes, Guid? contactId, Guid? opportunityId)
    {
        EnsureEditable();
        Title = NormalizeRequired(title, nameof(title));
        ValidUntilUtc = validUntilUtc;
        Currency = NormalizeRequired(currency, nameof(currency)).ToUpperInvariant();
        Discount = Math.Max(0, discount);
        Notes = NormalizeOptional(notes);
        ContactId = contactId;
        OpportunityId = opportunityId;
        Recalculate();
    }

    public QuoteItem AddItem(string description, decimal quantity, decimal unitPrice, decimal taxRate)
    {
        EnsureEditable();
        var item = new QuoteItem(Id, description, quantity, unitPrice, taxRate);
        _items.Add(item);
        Recalculate();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureEditable();
        var item = _items.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return;
        _items.Remove(item);
        Recalculate();
    }

    public void MarkSent()
    {
        if (_items.Count == 0) throw new InvalidOperationException("La cotización debe tener al menos una partida.");
        Status = "sent";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAccepted()
    {
        Status = "accepted";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRejected()
    {
        Status = "rejected";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = "cancelled";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Recalculate()
    {
        Subtotal = _items.Sum(x => x.Subtotal);
        Tax = _items.Sum(x => x.Tax);
        Total = Math.Max(0, Subtotal + Tax - Discount);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureEditable()
    {
        if (Status is "accepted" or "cancelled")
            throw new InvalidOperationException("La cotización ya no puede modificarse.");
    }

    private static string NormalizeRequired(string value, string name)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("El valor es obligatorio.", name);
        return normalized;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class QuoteItem
{
    private QuoteItem() { }

    public QuoteItem(Guid quoteId, string description, decimal quantity, decimal unitPrice, decimal taxRate)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (taxRate < 0) throw new ArgumentOutOfRangeException(nameof(taxRate));

        Id = Guid.NewGuid();
        QuoteId = quoteId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        Subtotal = quantity * unitPrice;
        Tax = Subtotal * taxRate / 100m;
        Total = Subtotal + Tax;
    }

    public Guid Id { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Total { get; private set; }
}
