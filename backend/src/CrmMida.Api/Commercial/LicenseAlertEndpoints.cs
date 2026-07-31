using System.Data;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class LicenseAlertEndpoints
{
    public static void MapLicenseAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/licenses/alerts").RequireAuthorization("licenses.manage");

        group.MapPost("/process", async (ProcessLicenseAlertsRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var days = Math.Clamp(request.Days, 1, 365);
            var today = DateTime.UtcNow.Date;
            var candidates = await ReadCandidatesAsync(db, today.AddDays(days), ct);
            var created = 0;
            var skipped = 0;

            foreach (var row in candidates)
            {
                var alertType = ResolveAlertType(row.ExpiresAtUtc, today);
                if (await WasDispatchedAsync(db, row.Id, alertType, today, ct))
                {
                    skipped++;
                    continue;
                }

                var due = row.ExpiresAtUtc < today ? today : row.ExpiresAtUtc.AddDays(-7);
                if (due < today) due = today;

                var activity = new Activity("task", $"Renovar {row.ProductName} · {row.SerialNumber}", due, row.AssignedUserId);
                activity.Update(
                    "task",
                    activity.Subject,
                    $"Licencia de {row.CompanyName}. Vence el {row.ExpiresAtUtc:dd/MM/yyyy}. Estado de alerta: {alertType}.",
                    due,
                    row.ExpiresAtUtc < today ? "high" : "normal",
                    "pending",
                    row.AssignedUserId,
                    null,
                    null,
                    row.CompanyId);
                db.Activities.Add(activity);
                await db.SaveChangesAsync(ct);

                await InsertDispatchAsync(db, row.Id, alertType, today, activity.Id, ct);
                created++;
            }

            return Results.Ok(new { evaluated = candidates.Count, created, skipped, days });
        });

        group.MapGet("/history", async (Guid? licenseId, ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = new List<LicenseAlertHistoryDto>();
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT d."Id", d."LicenseId", l."ProductName", l."SerialNumber", d."AlertType",
                       d."AlertDateUtc", d."ActivityId", d."CreatedAtUtc"
                FROM license_alert_dispatches d
                INNER JOIN licenses l ON l."Id" = d."LicenseId"
                WHERE (@licenseId IS NULL OR d."LicenseId" = @licenseId)
                ORDER BY d."CreatedAtUtc" DESC;
                """;
            Add(command, "@licenseId", licenseId.HasValue ? licenseId.Value : DBNull.Value);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new LicenseAlertHistoryDto(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDateTime(5), reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.GetDateTime(7)));
            return Results.Ok(rows);
        });
    }

    private static async Task<List<LicenseAlertCandidate>> ReadCandidatesAsync(ApplicationDbContext db, DateTime limit, CancellationToken ct)
    {
        var rows = new List<LicenseAlertCandidate>();
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l."Id", l."CompanyId", c."TradeName", l."ProductName", l."SerialNumber", l."ExpiresAtUtc", c."AssignedUserId"
            FROM licenses l INNER JOIN companies c ON c."Id" = l."CompanyId"
            WHERE l."ExpiresAtUtc" <= @limit
            ORDER BY l."ExpiresAtUtc";
            """;
        Add(command, "@limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new LicenseAlertCandidate(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDateTime(5), reader.IsDBNull(6) ? null : reader.GetGuid(6)));
        return rows;
    }

    private static string ResolveAlertType(DateTime expires, DateTime today)
    {
        var days = (expires.Date - today).Days;
        if (days < 0) return "expired";
        if (days <= 30) return "30_days";
        if (days <= 60) return "60_days";
        return "90_days";
    }

    private static async Task<bool> WasDispatchedAsync(ApplicationDbContext db, Guid licenseId, string alertType, DateTime date, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM license_alert_dispatches WHERE \"LicenseId\"=@licenseId AND \"AlertType\"=@type AND \"AlertDateUtc\"=@date;";
        Add(command, "@licenseId", licenseId); Add(command, "@type", alertType); Add(command, "@date", date);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) > 0;
    }

    private static async Task InsertDispatchAsync(ApplicationDbContext db, Guid licenseId, string alertType, DateTime date, Guid activityId, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO license_alert_dispatches (\"Id\",\"LicenseId\",\"AlertType\",\"AlertDateUtc\",\"ActivityId\",\"CreatedAtUtc\") VALUES (@id,@licenseId,@type,@date,@activityId,@created);";
        Add(command, "@id", Guid.NewGuid()); Add(command, "@licenseId", licenseId); Add(command, "@type", alertType); Add(command, "@date", date); Add(command, "@activityId", activityId); Add(command, "@created", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }

    private sealed record LicenseAlertCandidate(Guid Id, Guid CompanyId, string CompanyName, string ProductName, string SerialNumber, DateTime ExpiresAtUtc, Guid? AssignedUserId);
}

public sealed record ProcessLicenseAlertsRequest(int Days = 90);
public sealed record LicenseAlertHistoryDto(Guid Id, Guid LicenseId, string ProductName, string SerialNumber, string AlertType, DateTime AlertDateUtc, Guid? ActivityId, DateTime CreatedAtUtc);
