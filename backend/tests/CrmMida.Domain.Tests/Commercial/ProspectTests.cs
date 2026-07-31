using CrmMida.Domain.Commercial;
using Xunit;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class ProspectTests
{
    [Fact]
    public void Prospect_normalizes_contact_data()
    {
        var prospect = new Prospect(" Octavio Ortiz ", " Referido ");

        prospect.Update(
            " Octavio Ortiz ",
            " MIDA ",
            " mid010101abc ",
            " OCTAVIO@MIDA.MX ",
            " 4770000000 ",
            " Referido ",
            " Comercial Premium ",
            " CONTACTED ",
            " HOT ",
            " Seguimiento prioritario ",
            null);

        Assert.Equal("Octavio Ortiz", prospect.Name);
        Assert.Equal("MID010101ABC", prospect.Rfc);
        Assert.Equal("octavio@mida.mx", prospect.Email);
        Assert.Equal("contacted", prospect.Status);
        Assert.Equal("hot", prospect.Qualification);
    }

    [Fact]
    public void Prospect_can_be_marked_as_converted()
    {
        var prospect = new Prospect("Cliente Demo", "Web");
        var companyId = Guid.NewGuid();

        prospect.MarkConverted(companyId);

        Assert.Equal("converted", prospect.Status);
        Assert.Equal(companyId, prospect.ConvertedCompanyId);
        Assert.NotNull(prospect.ConvertedAtUtc);
    }

    [Fact]
    public void Prospect_deactivation_marks_it_as_discarded()
    {
        var prospect = new Prospect("Cliente Demo", "Web");

        prospect.Deactivate();

        Assert.False(prospect.IsActive);
        Assert.Equal("discarded", prospect.Status);
    }
}
