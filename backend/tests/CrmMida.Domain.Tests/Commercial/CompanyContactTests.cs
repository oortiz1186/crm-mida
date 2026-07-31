using CrmMida.Domain.Commercial;

namespace CrmMida.Domain.Tests.Commercial;

public sealed class CompanyContactTests
{
    [Fact]
    public void Company_normalizes_rfc_and_optional_values()
    {
        var company = new Company(" MIDA ", " MIDA SA de CV ", " mid010101abc ", "Cliente");

        company.Update(
            " MIDA ",
            " MIDA SA de CV ",
            " mid010101abc ",
            " 601 ",
            " 37000 ",
            " ADMIN@MIDA.MX ",
            " 4770000000 ",
            null,
            null,
            " León ",
            " Guanajuato ",
            " Cliente ",
            " ACTIVE ",
            " VIP ",
            null,
            null);

        Assert.Equal("MID010101ABC", company.Rfc);
        Assert.Equal("admin@mida.mx", company.Email);
        Assert.Equal("active", company.Status);
        Assert.Equal("León", company.City);
    }

    [Fact]
    public void Company_deactivation_sets_inactive_state()
    {
        var company = new Company("MIDA", "MIDA SA de CV", "MID010101ABC", "Cliente");

        company.Deactivate();

        Assert.False(company.IsActive);
        Assert.Equal("inactive", company.Status);
    }

    [Fact]
    public void Contact_can_change_primary_state_and_deactivate()
    {
        var contact = new Contact(Guid.NewGuid(), " Octavio ", " Ortiz ", " OCTAVIO@MIDA.MX ");

        contact.SetPrimary(true);
        contact.Deactivate();

        Assert.Equal("Octavio", contact.FirstName);
        Assert.Equal("octavio@mida.mx", contact.Email);
        Assert.True(contact.IsPrimary);
        Assert.False(contact.IsActive);
    }
}
