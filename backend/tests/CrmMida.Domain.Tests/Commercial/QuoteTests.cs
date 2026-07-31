using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class QuoteTests
{
    [Fact]
    public void Quote_calculates_subtotal_tax_and_total()
    {
        var quote = new Quote(Guid.NewGuid(), "Renovación", DateTime.UtcNow.AddDays(15));
        quote.SetFolio("cot-2026-000001");
        quote.Update("Renovación", DateTime.UtcNow.AddDays(15), "mxn", 100m, null, null, null);
        quote.AddItem("Licencia", 2m, 1000m, 16m);

        Assert.Equal("COT-2026-000001", quote.Folio);
        Assert.Equal(2000m, quote.Subtotal);
        Assert.Equal(320m, quote.Tax);
        Assert.Equal(2220m, quote.Total);
    }

    [Fact]
    public void Quote_cannot_be_sent_without_items()
    {
        var quote = new Quote(Guid.NewGuid(), "Renovación", DateTime.UtcNow.AddDays(15));
        Assert.Throws<InvalidOperationException>(() => quote.MarkSent());
    }

    [Fact]
    public void Accepted_quote_cannot_be_edited()
    {
        var quote = new Quote(Guid.NewGuid(), "Renovación", DateTime.UtcNow.AddDays(15));
        quote.AddItem("Licencia", 1m, 1000m, 16m);
        quote.MarkAccepted();

        Assert.Throws<InvalidOperationException>(() => quote.Update("Cambio", DateTime.UtcNow.AddDays(10), "MXN", 0, null, null, null));
    }
}
