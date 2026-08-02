using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class LicenseDashboardEndpoints
{
    public static void MapLicenseDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/licenses").RequireAuthorization();

        group.MapGet("/dashboard", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                  COUNT(*) FILTER (WHERE "ExpiresAtUtc" < NOW()) AS expired,
                  COUNT(*) FILTER (WHERE "ExpiresAtUtc" >= NOW() AND "ExpiresAtUtc" <= NOW() + INTERVAL '30 days') AS d30,
                  COUNT(*) FILTER (WHERE "ExpiresAtUtc" > NOW() + INTERVAL '30 days' AND "ExpiresAtUtc" <= NOW() + INTERVAL '60 days') AS d60,
                  COUNT(*) FILTER (WHERE "ExpiresAtUtc" > NOW() + INTERVAL '60 days' AND "ExpiresAtUtc" <= NOW() + INTERVAL '90 days') AS d90,
                  COUNT(*) FILTER (WHERE "ExpiresAtUtc" > NOW() + INTERVAL '90 days') AS active,
                  COUNT(*) AS total
                FROM licenses;
                """;
            await using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            return Results.Ok(new
            {
                expired = reader.GetInt64(0),
                expiring30 = reader.GetInt64(1),
                expiring60 = reader.GetInt64(2),
                expiring90 = reader.GetInt64(3),
                active = reader.GetInt64(4),
                total = reader.GetInt64(5)
            });
        }).RequireAuthorization("licenses.read");

        group.MapGet("/{id:guid}/renewals", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = new List<RenewalHistoryDto>();
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id","TargetDateUtc","EstimatedAmount","Status","OpportunityId","Notes","CreatedAtUtc","CompletedAtUtc"
                FROM renewal_opportunities WHERE "LicenseId"=@id ORDER BY "CreatedAtUtc" DESC;
                """;
            var parameter = command.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = id; command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new RenewalHistoryDto(reader.GetGuid(0), reader.GetDateTime(1), reader.GetDecimal(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetGuid(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetDateTime(7)));
            return Results.Ok(rows);
        }).RequireAuthorization("licenses.read");

        group.MapGet("/alerts", async (int? days, ApplicationDbContext db, CancellationToken ct) =>
        {
            var horizon = Math.Clamp(days ?? 90, 1, 365);
            var rows = new List<object>();
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT l."Id",c."TradeName",l."ProductName",l."SerialNumber",l."ExpiresAtUtc",
                       (l."ExpiresAtUtc"::date - CURRENT_DATE) AS days_left
                FROM licenses l INNER JOIN companies c ON c."Id"=l."CompanyId"
                WHERE l."ExpiresAtUtc" <= NOW() + (@days || ' days')::interval
                ORDER BY l."ExpiresAtUtc";
                """;
            var parameter = command.CreateParameter(); parameter.ParameterName = "@days"; parameter.Value = horizon; command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) rows.Add(new { id = reader.GetGuid(0), companyName = reader.GetString(1), productName = reader.GetString(2), serialNumber = reader.GetString(3), expiresAtUtc = reader.GetDateTime(4), daysToExpire = reader.GetInt32(5) });
            return Results.Ok(rows);
        }).RequireAuthorization("licenses.read");
    }
}

public sealed record RenewalHistoryDto(Guid Id, DateTime TargetDateUtc, decimal EstimatedAmount, string Status, Guid? OpportunityId, string? Notes, DateTime CreatedAtUtc, DateTime? CompletedAtUtc);
