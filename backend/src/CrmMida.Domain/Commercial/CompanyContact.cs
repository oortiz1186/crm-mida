using System.ComponentModel.DataAnnotations.Schema;
using CrmMida.Domain.Common;

namespace CrmMida.Domain.Commercial;

[Table("company_contacts")]
public sealed class CompanyContact : BaseEntity
{
    private CompanyContact() { }

    public CompanyContact(Guid companyId, Guid contactId, bool isPrimary = false)
    {
        CompanyId = companyId;
        ContactId = contactId;
        IsPrimary = isPrimary;
        Active = true;
    }

    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid ContactId { get; private set; }
    public Contact Contact { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public bool Active { get; private set; }

    public void Update(bool isPrimary, bool active = true)
    {
        IsPrimary = isPrimary;
        Active = active;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
