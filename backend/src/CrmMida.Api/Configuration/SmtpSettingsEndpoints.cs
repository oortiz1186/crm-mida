using System.Net;
using System.Net.Mail;

namespace CrmMida.Api.Configuration;

public static class SmtpSettingsEndpoints
{
    public static void MapSmtpSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/administration/smtp").RequireAuthorization("companies.manage");

        group.MapGet("/", async (SmtpSettingsService service, CancellationToken ct) =>
        {
            var settings = await service.GetAsync(ct);
            return Results.Ok(new
            {
                settings.Host,
                settings.Port,
                settings.EnableSsl,
                settings.UserName,
                settings.FromEmail,
                settings.FromName,
                passwordConfigured = !string.IsNullOrWhiteSpace(settings.Password),
                settings.Configured
            });
        });

        group.MapPut("/", async (SaveSmtpSettingsRequest request, SmtpSettingsService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Host) || string.IsNullOrWhiteSpace(request.FromEmail))
                return Results.BadRequest(new { message = "Servidor SMTP y correo remitente son obligatorios." });
            if (request.Port <= 0 || request.Port > 65535)
                return Results.BadRequest(new { message = "El puerto SMTP no es válido." });

            await service.SaveAsync(new SmtpSettings(
                request.Host.Trim(), request.Port, request.EnableSsl,
                string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim(),
                request.Password,
                request.FromEmail.Trim(),
                string.IsNullOrWhiteSpace(request.FromName) ? null : request.FromName.Trim()),
                preservePassword: true, ct);

            return Results.Ok(new { message = "Configuración SMTP guardada correctamente." });
        });

        group.MapPost("/test", async (TestSmtpRequest request, SmtpSettingsService service, CancellationToken ct) =>
        {
            var settings = await service.GetAsync(ct);
            if (!settings.Configured)
                return Results.BadRequest(new { message = "Primero guarda una configuración SMTP válida." });
            if (string.IsNullOrWhiteSpace(request.Recipient))
                return Results.BadRequest(new { message = "Indica un correo destinatario para la prueba." });

            try
            {
                var from = string.IsNullOrWhiteSpace(settings.FromName)
                    ? new MailAddress(settings.FromEmail)
                    : new MailAddress(settings.FromEmail, settings.FromName);
                using var mail = new MailMessage { From = from, Subject = "CRM MIDA · Prueba SMTP", Body = "La configuración SMTP de CRM MIDA funciona correctamente.", IsBodyHtml = false };
                mail.To.Add(request.Recipient.Trim());
                using var client = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = string.IsNullOrWhiteSpace(settings.UserName)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(settings.UserName, settings.Password)
                };
                await client.SendMailAsync(mail, ct);
                return Results.Ok(new { message = $"Correo de prueba enviado a {request.Recipient.Trim()}." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"No fue posible enviar el correo de prueba: {ex.Message}" });
            }
        });
    }
}

public sealed record SaveSmtpSettingsRequest(string Host, int Port, bool EnableSsl, string? UserName, string? Password, string FromEmail, string? FromName);
public sealed record TestSmtpRequest(string Recipient);
