using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class LicenseEndpoints
{
    public static void MapLicenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/licenses").RequireAuthorization();

        group.MapGet("/", async (string? search, string? status, int? expiringInDays, ApplicationDbContext db, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var query = db.Licenses.AsNoTracking().Include(x => x.Company).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(x => x.ProductName.ToLower().Contains(term) || x.SerialNumber.ToLower().Contains(term) || x.Company!.TradeName.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLowerInvariant());
            if (expiringInDays.HasValue) query = query.Where(x => x.ExpiresAtUtc >= now && x.ExpiresAtUtc <= now.AddDays(Math.Clamp(expiringInDays.Value, 1, 365)));
            var rows = await query.OrderBy(x => x.ExpiresAtUtc).ToListAsync(ct);
            foreach (var row in rows) row.CalculateStatus(now);
            return Results.Ok(rows.Select(ToDto).ToArray());
        }).RequireAuthorization("licenses.read");

        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = await db.Licenses.AsNoTracking().Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            return license is null ? Results.NotFound() : Results.Ok(ToDto(license));
        }).RequireAuthorization("licenses.read");

        group.MapPost("/", async (SaveLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await db.Companies.AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) return Results.BadRequest(new { message = "La empresa no existe." });
            var serial = request.SerialNumber.Trim().ToUpperInvariant().Replace(" ", string.Empty);
            if (await db.Licenses.AnyAsync(x => x.SerialNumber == serial, ct)) return Results.Conflict(new { message = "El número de serie ya está registrado." });
            var license = new License(request.CompanyId, request.ProductName, serial, request.StartsAtUtc, request.ExpiresAtUtc, request.Users);
            license.Update(request.ProductName, request.Version, request.LicenseType, request.Users, request.Companies, request.StartsAtUtc, request.ExpiresAtUtc, request.Notes);
            db.Licenses.Add(license);
            await db.SaveChangesAsync(ct);
            await db.Entry(license).Reference(x => x.Company).LoadAsync(ct);
            return Results.Created($"/api/v1/licenses/{license.Id}", ToDto(license));
        }).RequireAuthorization("licenses.manage");

        group.MapPut("/{id:guid}", async (Guid id, SaveLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = await db.Licenses.Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (license is null) return Results.NotFound();
            license.Update(request.ProductName, request.Version, request.LicenseType, request.Users, request.Companies, request.StartsAtUtc, request.ExpiresAtUtc, request.Notes);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(license));
        }).RequireAuthorization("licenses.manage");

        group.MapPost("/{id:guid}/renewals", async (Guid id, CreateRenewalRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = await db.Licenses.Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (license is null) return Results.NotFound();
            if (await db.RenewalOpportunities.AnyAsync(x => x.LicenseId == id && x.Status == "pending", ct)) return Results.Conflict(new { message = "Ya existe una renovación pendiente." });

            var targetDate = request.TargetDateUtc ?? license.ExpiresAtUtc;
            var renewal = new RenewalOpportunity(id, targetDate, request.EstimatedAmount);
            var opportunity = new Opportunity($"Renovación {license.ProductName} · {license.SerialNumber}", license.CompanyId, request.EstimatedAmount);
            opportunity.Update(
                $"Renovación {license.ProductName} · {license.SerialNumber}",
                license.CompanyId,
                null,
                null,
                null,
                "Renovación de licencia",
                request.EstimatedAmount,
                50,
                targetDate,
                "prospecting",
                "open",
                null,
                $"Serie {license.SerialNumber}");
            db.Opportunities.Add(opportunity);
            renewal.LinkOpportunity(opportunity.Id);
            db.RenewalOpportunities.Add(renewal);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/licenses/{id}/renewals/{renewal.Id}", new { renewal.Id, renewal.Status, renewal.TargetDateUtc, renewal.EstimatedAmount, renewal.OpportunityId });
        }).RequireAuthorization("licenses.manage");

        group.MapPost("/{id:guid}/renew", async (Guid id, RenewLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = await db.Licenses.Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (license is null) return Results.NotFound();
            license.Renew(request.NewExpiresAtUtc);
            var renewal = await db.RenewalOpportunities.Where(x => x.LicenseId == id && x.Status == "pending").OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
            renewal?.Complete("renewed", request.Notes);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(license));
        }).RequireAuthorization("licenses.manage");
    }

    private static LicenseDto ToDto(License x) => new(x.Id, x.CompanyId, x.Company?.TradeName ?? string.Empty, x.ProductName, x.SerialNumber, x.Version, x.LicenseType, x.Users, x.Companies, x.StartsAtUtc, x.ExpiresAtUtc, x.Status, x.DaysToExpire(DateTime.UtcNow), x.Notes);
}

public sealed record SaveLicenseRequest(Guid CompanyId, string ProductName, string SerialNumber, string? Version, string? LicenseType, int Users, int Companies, DateTime StartsAtUtc, DateTime ExpiresAtUtc, string? Notes);
public sealed record CreateRenewalRequest(DateTime? TargetDateUtc, decimal EstimatedAmount = 0);
public sealed record RenewLicenseRequest(DateTime NewExpiresAtUtc, string? Notes);
public sealed record LicenseDto(Guid Id, Guid CompanyId, string CompanyName, string ProductName, string SerialNumber, string? Version, string? LicenseType, int Users, int Companies, DateTime StartsAtUtc, DateTime ExpiresAtUtc, string Status, int DaysToExpire, string? Notes);
