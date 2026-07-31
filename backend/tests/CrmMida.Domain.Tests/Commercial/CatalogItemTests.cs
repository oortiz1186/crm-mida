using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class CatalogItemTests
{
    [Fact]
    public void Catalog_item_normalizes_code_and_type()
    {
        var item = new CatalogItem(" srv-001 ", " Soporte remoto ", "SERVICE", 500m);

        Assert.Equal("SRV-001", item.Code);
        Assert.Equal("service", item.Type);
        Assert.Equal(500m, item.UnitPrice);
        Assert.True(item.IsActive);
    }

    [Fact]
    public void Catalog_item_rejects_invalid_tax_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CatalogItem("P-1", "Producto", "product", 10m, 101m));
    }

    [Fact]
    public void Catalog_item_can_be_deactivated()
    {
        var item = new CatalogItem("P-1", "Producto", "product", 10m);

        item.Deactivate();

        Assert.False(item.IsActive);
    }
}
