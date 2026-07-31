using CrmMida.Domain.Common;
using CrmMida.Domain.Security;

namespace CrmMida.Domain.Commercial;

public sealed class Opportunity : BaseEntity
{
    private Opportunity() { }

    public Opportunity(string name, Guid companyId, decimal estimatedAmount, Guid? assignedUserId = null)
    {
        Name = name.Trim();
        CompanyId = companyId;
        EstimatedAmount = estimatedAmount;
        AssignedUserId = assignedUserId;
    }

    public string Name { get; private set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public Guid? ContactId { get; private set; }
    public Contact? Contact { get; private set; }
    public Guid? ProspectId { get; private set; }
    public Prospect? Prospect { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public User? AssignedUser { get; private set; }
    public string? ProductOrService { get; private set; }
    public decimal EstimatedAmount { get; private set; }
    public int Probability { get; private set; }
    public DateTime? ExpectedCloseDateUtc { get; private set; }
    public string Stage { get; private set; } = "prospecting";
    public string Status { get; private set; } = "open";
    public string? LossReason { get; private set; }
    public string? Notes { get; private set; }
    public ICollection<Activity> Activities { get; private set; } = new List<Activity>();

    public void Update(
        string name,
        Guid companyId,
        Guid? contactId,
        Guid? prospectId,
        Guid? assignedUserId,
        string? productOrService,
        decimal estimatedAmount,
        int probability,
        DateTime? expectedCloseDateUtc,
        string stage,
        string status,
        string? lossReason,
        string? notes)
    {
        Name = name.Trim();
        CompanyId = companyId;
        ContactId = contactId;
        ProspectId = prospectId;
        AssignedUserId = assignedUserId;
        ProductOrService = Normalize(productOrService);
        EstimatedAmount = Math.Max(estimatedAmount, 0);
        Probability = Math.Clamp(probability, 0, 100);
        ExpectedCloseDateUtc = expectedCloseDateUtc;
        MoveToStage(stage, lossReason);
        Status = string.IsNullOrWhiteSpace(status) ? Status : status.Trim().ToLowerInvariant();
        Notes = Normalize(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MoveToStage(string stage, string? lossReason = null)
    {
        Stage = stage.Trim().ToLowerInvariant();
        LossReason = Stage == "lost" ? Normalize(lossReason) : null;
        Status = Stage switch
        {
            "won" => "won",
            "lost" => "lost",
            _ => "open"
        };
        Probability = Stage switch
        {
            "won" => 100,
            "lost" => 0,
            _ => Probability
        };
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = "cancelled";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
