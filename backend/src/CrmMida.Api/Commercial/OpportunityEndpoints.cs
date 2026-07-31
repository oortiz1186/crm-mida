using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class OpportunityEndpoints
{
    private static readonly string[] Stages = ["prospecting", "qualification", "diagnosis", "quotation", "negotiation", "won", "lost"];

    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var opportunities = endpoints.MapGroup("/api/v1/opportunities").WithTags("Opportunities");
        opportunities.MapGet("/", GetOpportunitiesAsync).RequireAuthorization("opportunities.read");
        opportunities.MapGet("/{id:guid}", GetOpportunityAsync).RequireAuthorization("opportunities.read");
        opportunities.MapPost("/", CreateOpportunityAsync).RequireAuthorization("opportunities.manage");
        opportunities.MapPut("/{id:guid}", UpdateOpportunityAsync).RequireAuthorization("opportunities.manage");
        opportunities.MapPatch("/{id:guid}/stage", MoveOpportunityAsync).RequireAuthorization("opportunities.manage");
        opportunities.MapDelete("/{id:guid}", DeleteOpportunityAsync).RequireAuthorization("opportunities.manage");

        var activities = endpoints.MapGroup("/api/v1/activities").WithTags("Activities");
        activities.MapGet("/", GetActivitiesAsync).RequireAuthorization("activities.read");
        activities.MapPost("/", CreateActivityAsync).RequireAuthorization("activities.manage");
        activities.MapPut("/{id:guid}", UpdateActivityAsync).RequireAuthorization("activities.manage");
        activities.MapPatch("/{id:guid}/status", UpdateActivityStatusAsync).RequireAuthorization("activities.manage");
        activities.MapDelete("/{id:guid}", DeleteActivityAsync).RequireAuthorization("activities.manage");
        return endpoints;
    }

    private static async Task<IResult> GetOpportunitiesAsync(ApplicationDbContext db, string? search, string? stage, CancellationToken ct)
    {
        var query = db.Opportunities.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Company.TradeName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage == stage.Trim().ToLowerInvariant());

        var items = await query.OrderBy(x => x.Stage).ThenByDescending(x => x.EstimatedAmount)
            .Select(x => new OpportunityDto(x.Id, x.Name, x.CompanyId, x.Company.TradeName, x.ContactId, x.ProspectId, x.AssignedUserId, x.ProductOrService, x.EstimatedAmount, x.Probability, x.ExpectedCloseDateUtc, x.Stage, x.Status, x.LossReason, x.Notes))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetOpportunityAsync(Guid id, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Opportunities.AsNoTracking().Where(x => x.Id == id && x.IsActive)
            .Select(x => new OpportunityDetailDto(
                new OpportunityDto(x.Id, x.Name, x.CompanyId, x.Company.TradeName, x.ContactId, x.ProspectId, x.AssignedUserId, x.ProductOrService, x.EstimatedAmount, x.Probability, x.ExpectedCloseDateUtc, x.Stage, x.Status, x.LossReason, x.Notes),
                x.Activities.Where(a => a.IsActive).OrderBy(a => a.DueAtUtc).Select(a => new ActivityDto(a.Id, a.Type, a.Subject, a.Description, a.DueAtUtc, a.Priority, a.Status, a.AssignedUserId, a.OpportunityId, a.ProspectId, a.CompanyId, a.CompletedAtUtc)).ToArray()))
            .SingleOrDefaultAsync(ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateOpportunityAsync(OpportunityRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var error = await ValidateOpportunityAsync(request, db, null, ct);
        if (error is not null) return error;
        var item = new Opportunity(request.Name, request.CompanyId, request.EstimatedAmount, request.AssignedUserId);
        item.Update(request.Name, request.CompanyId, request.ContactId, request.ProspectId, request.AssignedUserId, request.ProductOrService, request.EstimatedAmount, request.Probability, request.ExpectedCloseDateUtc, request.Stage, request.Status, request.LossReason, request.Notes);
        db.Opportunities.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/opportunities/{item.Id}", item.Id);
    }

    private static async Task<IResult> UpdateOpportunityAsync(Guid id, OpportunityRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Opportunities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        var error = await ValidateOpportunityAsync(request, db, id, ct);
        if (error is not null) return error;
        item.Update(request.Name, request.CompanyId, request.ContactId, request.ProspectId, request.AssignedUserId, request.ProductOrService, request.EstimatedAmount, request.Probability, request.ExpectedCloseDateUtc, request.Stage, request.Status, request.LossReason, request.Notes);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MoveOpportunityAsync(Guid id, StageRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Opportunities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        if (!Stages.Contains(request.Stage.Trim().ToLowerInvariant())) return Results.ValidationProblem(new Dictionary<string, string[]> { ["stage"] = ["Etapa no válida."] });
        if (request.Stage.Equals("lost", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LossReason)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["lossReason"] = ["Indica el motivo de pérdida."] });
        item.MoveToStage(request.Stage, request.LossReason);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteOpportunityAsync(Guid id, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Opportunities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        item.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult?> ValidateOpportunityAsync(OpportunityRequest request, ApplicationDbContext db, Guid? id, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["El nombre es obligatorio."];
        if (request.EstimatedAmount < 0) errors["estimatedAmount"] = ["El monto no puede ser negativo."];
        if (request.Probability is < 0 or > 100) errors["probability"] = ["La probabilidad debe estar entre 0 y 100."];
        if (!Stages.Contains(request.Stage.Trim().ToLowerInvariant())) errors["stage"] = ["Etapa no válida."];
        if (!await db.Companies.AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) errors["companyId"] = ["La empresa no existe."];
        if (request.ContactId.HasValue && !await db.Contacts.AnyAsync(x => x.Id == request.ContactId && x.CompanyId == request.CompanyId && x.IsActive, ct)) errors["contactId"] = ["El contacto no pertenece a la empresa."];
        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static async Task<IResult> GetActivitiesAsync(ApplicationDbContext db, Guid? opportunityId, string? status, CancellationToken ct)
    {
        var query = db.Activities.AsNoTracking().Where(x => x.IsActive);
        if (opportunityId.HasValue) query = query.Where(x => x.OpportunityId == opportunityId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLowerInvariant());
        var items = await query.OrderBy(x => x.DueAtUtc).Select(a => new ActivityDto(a.Id, a.Type, a.Subject, a.Description, a.DueAtUtc, a.Priority, a.Status, a.AssignedUserId, a.OpportunityId, a.ProspectId, a.CompanyId, a.CompletedAtUtc)).ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateActivityAsync(ActivityRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var error = ValidateActivity(request);
        if (error is not null) return error;
        var item = new Activity(request.Type, request.Subject, request.DueAtUtc, request.AssignedUserId);
        item.Update(request.Type, request.Subject, request.Description, request.DueAtUtc, request.Priority, request.Status, request.AssignedUserId, request.OpportunityId, request.ProspectId, request.CompanyId);
        db.Activities.Add(item);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/activities/{item.Id}", item.Id);
    }

    private static async Task<IResult> UpdateActivityAsync(Guid id, ActivityRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Activities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        var error = ValidateActivity(request);
        if (error is not null) return error;
        item.Update(request.Type, request.Subject, request.Description, request.DueAtUtc, request.Priority, request.Status, request.AssignedUserId, request.OpportunityId, request.ProspectId, request.CompanyId);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateActivityStatusAsync(Guid id, ActivityStatusRequest request, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Activities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        item.SetStatus(request.Status);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteActivityAsync(Guid id, ApplicationDbContext db, CancellationToken ct)
    {
        var item = await db.Activities.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (item is null) return Results.NotFound();
        item.Deactivate();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static IResult? ValidateActivity(ActivityRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Subject)) errors["subject"] = ["El asunto es obligatorio."];
        if (!request.OpportunityId.HasValue && !request.ProspectId.HasValue && !request.CompanyId.HasValue) errors["relation"] = ["Relaciona la actividad con una oportunidad, prospecto o empresa."];
        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }
}

public sealed record OpportunityRequest(string Name, Guid CompanyId, Guid? ContactId, Guid? ProspectId, Guid? AssignedUserId, string? ProductOrService, decimal EstimatedAmount, int Probability, DateTime? ExpectedCloseDateUtc, string Stage, string Status, string? LossReason, string? Notes);
public sealed record StageRequest(string Stage, string? LossReason);
public sealed record OpportunityDto(Guid Id, string Name, Guid CompanyId, string CompanyName, Guid? ContactId, Guid? ProspectId, Guid? AssignedUserId, string? ProductOrService, decimal EstimatedAmount, int Probability, DateTime? ExpectedCloseDateUtc, string Stage, string Status, string? LossReason, string? Notes);
public sealed record OpportunityDetailDto(OpportunityDto Opportunity, IReadOnlyCollection<ActivityDto> Activities);
public sealed record ActivityRequest(string Type, string Subject, string? Description, DateTime DueAtUtc, string Priority, string Status, Guid? AssignedUserId, Guid? OpportunityId, Guid? ProspectId, Guid? CompanyId);
public sealed record ActivityStatusRequest(string Status);
public sealed record ActivityDto(Guid Id, string Type, string Subject, string? Description, DateTime DueAtUtc, string Priority, string Status, Guid? AssignedUserId, Guid? OpportunityId, Guid? ProspectId, Guid? CompanyId, DateTime? CompletedAtUtc);
