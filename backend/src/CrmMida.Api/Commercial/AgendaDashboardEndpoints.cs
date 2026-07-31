using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class AgendaDashboardEndpoints
{
    public static void MapAgendaDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/workspace").RequireAuthorization("activities.read");

        group.MapGet("/dashboard", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var nextWeek = today.AddDays(7);

            var activities = await db.Activities.AsNoTracking()
                .Where(x => x.IsActive && x.Status != "completed" && x.Status != "cancelled")
                .OrderBy(x => x.DueAtUtc)
                .Take(100)
                .Select(x => new
                {
                    x.Id, x.Type, x.Subject, x.Description, x.DueAtUtc,
                    x.Priority, x.Status, x.CompanyId, x.OpportunityId, x.AssignedUserId
                })
                .ToListAsync(ct);

            var opportunities = await db.Opportunities.AsNoTracking()
                .Where(x => x.IsActive && x.Status == "open")
                .OrderBy(x => x.ExpectedCloseDateUtc)
                .Select(x => new
                {
                    x.Id, x.Name, x.CompanyId, x.Stage, x.EstimatedAmount,
                    x.Probability, x.ExpectedCloseDateUtc, x.AssignedUserId
                })
                .ToListAsync(ct);

            var quotes = await db.Quotes.AsNoTracking()
                .Where(x => x.Status == "sent" || x.Status == "draft")
                .OrderBy(x => x.ValidUntilUtc)
                .Select(x => new { x.Id, x.Folio, x.CompanyId, x.Title, x.Total, x.Status, x.ValidUntilUtc })
                .Take(50)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                generatedAtUtc = now,
                summary = new
                {
                    overdueActivities = activities.Count(x => x.DueAtUtc < now),
                    todayActivities = activities.Count(x => x.DueAtUtc >= today && x.DueAtUtc < today.AddDays(1)),
                    nextSevenDays = activities.Count(x => x.DueAtUtc >= today && x.DueAtUtc < nextWeek),
                    openOpportunities = opportunities.Count,
                    weightedPipeline = opportunities.Sum(x => x.EstimatedAmount * x.Probability / 100m),
                    expiringQuotes = quotes.Count(x => x.ValidUntilUtc < nextWeek)
                },
                priorities = new
                {
                    overdue = activities.Where(x => x.DueAtUtc < now).Take(20),
                    today = activities.Where(x => x.DueAtUtc >= today && x.DueAtUtc < today.AddDays(1)).Take(20),
                    upcoming = activities.Where(x => x.DueAtUtc >= today.AddDays(1) && x.DueAtUtc < nextWeek).Take(20),
                    opportunities = opportunities.Take(20),
                    quotes = quotes.Take(20)
                }
            });
        });

        group.MapGet("/agenda", async (DateTime? from, DateTime? to, string? status, ApplicationDbContext db, CancellationToken ct) =>
        {
            var start = (from ?? DateTime.UtcNow.Date.AddDays(-7)).ToUniversalTime();
            var end = (to ?? DateTime.UtcNow.Date.AddDays(30)).ToUniversalTime();
            var query = db.Activities.AsNoTracking().Where(x => x.IsActive && x.DueAtUtc >= start && x.DueAtUtc <= end);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLowerInvariant());
            var rows = await query.OrderBy(x => x.DueAtUtc).Select(x => new
            {
                x.Id, x.Type, x.Subject, x.Description, x.DueAtUtc, x.Priority,
                x.Status, x.CompanyId, x.OpportunityId, x.ProspectId, x.AssignedUserId
            }).ToListAsync(ct);
            return Results.Ok(rows);
        });
    }
}
