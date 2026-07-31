using CrmMida.Domain.Common;
using CrmMida.Domain.Security;

namespace CrmMida.Domain.Commercial;

public sealed class Activity : BaseEntity
{
    private Activity() { }

    public Activity(string type, string subject, DateTime dueAtUtc, Guid? assignedUserId = null)
    {
        Type = type.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        DueAtUtc = dueAtUtc;
        AssignedUserId = assignedUserId;
    }

    public string Type { get; private set; } = "task";
    public string Subject { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public string Priority { get; private set; } = "normal";
    public string Status { get; private set; } = "pending";
    public Guid? AssignedUserId { get; private set; }
    public User? AssignedUser { get; private set; }
    public Guid? OpportunityId { get; private set; }
    public Opportunity? Opportunity { get; private set; }
    public Guid? ProspectId { get; private set; }
    public Prospect? Prospect { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public void Update(
        string type,
        string subject,
        string? description,
        DateTime dueAtUtc,
        string priority,
        string status,
        Guid? assignedUserId,
        Guid? opportunityId,
        Guid? prospectId,
        Guid? companyId)
    {
        Type = type.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        Description = Normalize(description);
        DueAtUtc = dueAtUtc;
        Priority = priority.Trim().ToLowerInvariant();
        AssignedUserId = assignedUserId;
        OpportunityId = opportunityId;
        ProspectId = prospectId;
        CompanyId = companyId;
        SetStatus(status);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(string status)
    {
        Status = status.Trim().ToLowerInvariant();
        CompletedAtUtc = Status == "completed" ? DateTime.UtcNow : null;
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
