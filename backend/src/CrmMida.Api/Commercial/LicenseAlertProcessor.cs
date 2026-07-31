using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public sealed class LicenseAlertProcessor(
    ApplicationDbContext db,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<LicenseAlertProcessor> logger)
{
    public async Task<LicenseAlertProcessResult> ProcessAsync(int requestedDays, CancellationToken ct)
    {
        var days = Math.Clamp(requestedDays, 1, 365);
        var today = DateTime.UtcNow.Date;
        var candidates = await ReadCandidatesAsync(today.AddDays(days), ct);
        var created = 0;
        var skipped = 0;
        var notified = 0;
        var notificationFailures = 0;

        foreach (var row in candidates)
        {
            var alertType = ResolveAlertType(row.ExpiresAtUtc, today);
            if (await WasDispatchedAsync(row.Id, alertType, today, ct))
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
            await InsertDispatchAsync(row.Id, alertType, today, activity.Id, ct);
            created++;

            var results = await NotifyAsync(row, alertType, ct);
            notified += results.Count(x => x);
            notificationFailures += results.Count(x => !x);
        }

        return new LicenseAlertProcessResult(candidates.Count, created, skipped, notified, notificationFailures, days);
    }

    private async Task<IReadOnlyList<bool>> NotifyAsync(LicenseAlertCandidate row, string alertType, CancellationToken ct)
    {
        var results = new List<bool>();
        var message = $"Alerta de renovación MIDA\n\nEmpresa: {row.CompanyName}\nProducto: {row.ProductName}\nSerie: {row.SerialNumber}\nVencimiento: {row.ExpiresAtUtc:dd/MM/yyyy}\nNivel: {alertType}.";

        if (configuration.GetValue("LicenseAlerts:NotifyEmail", false) && !string.IsNullOrWhiteSpace(row.AssignedUserEmail))
            results.Add(await SendEmailAsync(row.AssignedUserEmail, $"Renovación {row.ProductName} · {row.CompanyName}", message, ct));

        if (configuration.GetValue("LicenseAlerts:NotifyWhatsApp", false) && !string.IsNullOrWhiteSpace(row.PrimaryMobile))
            results.Add(await SendWhatsAppAsync(row.PrimaryMobile, message, ct));

        return results;
    }

    private async Task<bool> SendEmailAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        var host = configuration["QuoteDelivery:Smtp:Host"];
        var from = configuration["QuoteDelivery:Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)) return false;

        try
        {
            using var mail = new MailMessage(from, recipient) { Subject = subject, Body = body };
            using var client = new SmtpClient(host, configuration.GetValue("QuoteDelivery:Smtp:Port", 587))
            {
                EnableSsl = configuration.GetValue("QuoteDelivery:Smtp:EnableSsl", true),
                Credentials = string.IsNullOrWhiteSpace(configuration["QuoteDelivery:Smtp:User"])
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(configuration["QuoteDelivery:Smtp:User"], configuration["QuoteDelivery:Smtp:Password"])
            };
            await client.SendMailAsync(mail, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar la alerta de licencia por correo a {Recipient}", recipient);
            return false;
        }
    }

    private async Task<bool> SendWhatsAppAsync(string recipient, string message, CancellationToken ct)
    {
        var baseUrl = configuration["QuoteDelivery:Evolution:BaseUrl"];
        var instance = configuration["QuoteDelivery:Evolution:Instance"];
        var apiKey = configuration["QuoteDelivery:Evolution:ApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(apiKey)) return false;

        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("apikey", apiKey);
            var number = new string(recipient.Where(char.IsDigit).ToArray());
            var response = await client.PostAsJsonAsync($"message/sendText/{instance}", new { number, text = message }, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo enviar la alerta de licencia por WhatsApp a {Recipient}", recipient);
            return false;
        }
    }

    private async Task<List<LicenseAlertCandidate>> ReadCandidatesAsync(DateTime limit, CancellationToken ct)
    {
        var rows = new List<LicenseAlertCandidate>();
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l."Id", l."CompanyId", c."TradeName", l."ProductName", l."SerialNumber", l."ExpiresAtUtc",
                   c."AssignedUserId", u."Email",
                   (SELECT COALESCE(NULLIF(ct."Mobile",''), ct."Phone") FROM contacts ct
                    WHERE ct."CompanyId"=c."Id" AND ct."IsActive"=TRUE
                    ORDER BY ct."IsPrimary" DESC, ct."CreatedAtUtc" LIMIT 1)
            FROM licenses l
            INNER JOIN companies c ON c."Id" = l."CompanyId"
            LEFT JOIN users u ON u."Id" = c."AssignedUserId"
            WHERE l."ExpiresAtUtc" <= @limit
            ORDER BY l."ExpiresAtUtc";
            """;
        Add(command, "@limit", limit);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new LicenseAlertCandidate(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)));
        return rows;
    }

    private async Task<bool> WasDispatchedAsync(Guid licenseId, string alertType, DateTime date, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM license_alert_dispatches WHERE \"LicenseId\"=@licenseId AND \"AlertType\"=@type AND \"AlertDateUtc\"=@date;";
        Add(command, "@licenseId", licenseId); Add(command, "@type", alertType); Add(command, "@date", date);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct)) > 0;
    }

    private async Task InsertDispatchAsync(Guid licenseId, string alertType, DateTime date, Guid activityId, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO license_alert_dispatches (\"Id\",\"LicenseId\",\"AlertType\",\"AlertDateUtc\",\"ActivityId\",\"CreatedAtUtc\") VALUES (@id,@licenseId,@type,@date,@activityId,@created);";
        Add(command, "@id", Guid.NewGuid()); Add(command, "@licenseId", licenseId); Add(command, "@type", alertType); Add(command, "@date", date); Add(command, "@activityId", activityId); Add(command, "@created", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string ResolveAlertType(DateTime expires, DateTime today)
    {
        var days = (expires.Date - today).Days;
        if (days < 0) return "expired";
        if (days <= 30) return "30_days";
        if (days <= 60) return "60_days";
        return "90_days";
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }

    private sealed record LicenseAlertCandidate(Guid Id, Guid CompanyId, string CompanyName, string ProductName, string SerialNumber, DateTime ExpiresAtUtc, Guid? AssignedUserId, string? AssignedUserEmail, string? PrimaryMobile);
}

public sealed record LicenseAlertProcessResult(int Evaluated, int Created, int Skipped, int Notified, int NotificationFailures, int Days);
