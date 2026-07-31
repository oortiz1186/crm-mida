using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class LicenseTests
{
    [Fact]
    public void License_normalizes_serial_and_calculates_expiring_status()
    {
        var now = DateTime.UtcNow;
        var license = new License(Guid.NewGuid(), "CONTPAQi Comercial Premium", " abc 123 ", now.AddMonths(-11), now.AddDays(20), 2);

        Assert.Equal("ABC123", license.SerialNumber);
        Assert.Equal("expiring", license.CalculateStatus(now));
        Assert.InRange(license.DaysToExpire(now), 19, 20);
    }

    [Fact]
    public void License_rejects_invalid_vigency()
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentException>(() => new License(Guid.NewGuid(), "CONTPAQi", "ABC", now, now.AddDays(-1)));
    }

    [Fact]
    public void License_can_be_renewed_only_forward()
    {
        var now = DateTime.UtcNow;
        var license = new License(Guid.NewGuid(), "CONTPAQi", "ABC", now.AddYears(-1), now.AddDays(5));

        license.Renew(now.AddYears(1));

        Assert.Equal("active", license.CalculateStatus(now));
        Assert.Throws<ArgumentException>(() => license.Renew(now));
    }

    [Fact]
    public void Renewal_can_link_opportunity_and_complete()
    {
        var renewal = new RenewalOpportunity(Guid.NewGuid(), DateTime.UtcNow.AddDays(30), 15000m);
        var opportunityId = Guid.NewGuid();

        renewal.LinkOpportunity(opportunityId);
        renewal.Complete("renewed", "Actualización anual");

        Assert.Equal(opportunityId, renewal.OpportunityId);
        Assert.Equal("renewed", renewal.Status);
        Assert.NotNull(renewal.CompletedAtUtc);
    }
}
