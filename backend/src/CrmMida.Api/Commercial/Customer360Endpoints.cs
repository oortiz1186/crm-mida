using System.Data;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class Customer360Endpoints
{
    public static void MapCustomer360Endpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/customers/{companyId:guid}/360", async (
            Guid companyId,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var company = await db.Companies
                .AsNoTracking()
                .Where(x => x.Id == companyId && x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.TradeName,
                    x.BusinessName,
                    x.Rfc,
                    x.CustomerType,
                    x.Status,
                    x.Email,
                    x.Phone,
                    x.Website,
                    x.Address,
                    x.City,
                    x.State,
                    x.Tags,
                    x.AssignedUserId
                })
                .SingleOrDefaultAsync(ct);

            if (company is null) return Results.NotFound();

            var contacts = await db.Contacts.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.FirstName)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Position,
                    x.Area,
                    x.Email,
                    x.Phone,
                    x.Mobile,
                    x.IsPrimary,
                    x.IsPurchasingContact,
                    x.IsTechnicalContact,
                    x.IsBillingContact
                })
                .ToListAsync(ct);

            var opportunities = await db.Opportunities.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ProductOrService,
                    x.EstimatedAmount,
                    x.Probability,
                    x.ExpectedCloseDateUtc,
                    x.Stage,
                    x.Status,
                    x.LossReason
                })
                .ToListAsync(ct);

            var quotes = await db.Quotes.AsNoTracking()
                .Where(x => x.CompanyId == companyId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Folio,
                    x.Title,
                    x.Currency,
                    x.Total,
                    x.Status,
                    x.ValidUntilUtc,
                    x.CreatedAtUtc
                })
                .ToListAsync(ct);

            var activities = await db.Activities.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.IsActive)
                .OrderBy(x => x.Status == "completed")
                .ThenBy(x => x.DueAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.Subject,
                    x.Description,
                    x.DueAtUtc,
                    x.Priority,
                    x.Status,
                    x.CompletedAtUtc
                })
                .Take(100)
                .ToListAsync(ct);

            var licenses = await ReadLicensesAsync(db, companyId, ct);
            var openPipeline = opportunities.Where(x => x.Status == "open").Sum(x => x.EstimatedAmount);
            var acceptedQuotes = quotes.Where(x => x.Status == "accepted").Sum(x => x.Total);

            return Results.Ok(new
            {
                company,
                summary = new
                {
                    contacts = contacts.Count,
                    openOpportunities = opportunities.Count(x => x.Status == "open"),
                    openPipeline,
                    pendingActivities = activities.Count(x => x.Status == "pending"),
                    quotes = quotes.Count,
                    acceptedQuotes,
                    licenses = licenses.Count,
                    expiringLicenses = licenses.Count(x => x.DaysToExpire <= 90)
                },
                contacts,
                opportunities,
                quotes,
                activities,
                licenses
            });
        }).RequireAuthorization("companies.read");
    }

    private static async Task<List<CustomerLicenseDto>> ReadLicensesAsync(
        ApplicationDbContext db,
        Guid companyId,
        CancellationToken ct)
    {
        var rows = new List<CustomerLicenseDto>();
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l."Id", l."ProductName", l."SerialNumber", l."Version", l."LicenseType",
                   l."Users", l."Companies", l."ExpiresAtUtc", l."Status",
                   (SELECT COUNT(*) FROM renewal_opportunities r
                    WHERE r."LicenseId" = l."Id" AND r."Status" = 'pending') AS "PendingRenewals"
            FROM licenses l
            WHERE l."CompanyId" = @companyId
            ORDER BY l."ExpiresAtUtc";
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@companyId";
        parameter.Value = companyId;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var expires = reader.GetDateTime(7);
            rows.Add(new CustomerLicenseDto(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetInt32(5), reader.GetInt32(6), expires,
                expires < DateTime.UtcNow ? "expired" : (expires.Date - DateTime.UtcNow.Date).TotalDays <= 30 ? "expiring" : "active",
                (int)Math.Ceiling((expires.Date - DateTime.UtcNow.Date).TotalDays),
                Convert.ToInt32(reader.GetValue(9))));
        }
        return rows;
    }
}

public sealed record CustomerLicenseDto(
    Guid Id,
    string ProductName,
    string SerialNumber,
    string? Version,
    string? LicenseType,
    int Users,
    int Companies,
    DateTime ExpiresAtUtc,
    string Status,
    int DaysToExpire,
    int PendingRenewals);
