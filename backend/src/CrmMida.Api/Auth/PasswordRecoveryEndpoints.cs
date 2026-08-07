using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CrmMida.Application.Security;
using CrmMida.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Auth;

public static class PasswordRecoveryEndpoints
{
    public static void MapPasswordRecoveryEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/forgot-password", ForgotPasswordAsync);
        app.MapPost("/api/v1/auth/reset-password", ResetPasswordAsync);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        ApplicationDbContext db,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken ct)
    {
        const string genericMessage = "Si el correo está registrado, recibirás instrucciones para restablecer tu contraseña.";
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return Results.Ok(new { message = genericMessage });

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, ct);
        if (user is null)
            return Results.Ok(new { message = genericMessage });

        var expiresUtc = DateTime.UtcNow.AddMinutes(30);
        var token = CreateToken(user.Id, user.PasswordHash, expiresUtc, configuration);
        var frontendUrl = (configuration["PasswordRecovery:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        var smtpHost = configuration["QuoteDelivery:Smtp:Host"];
        var smtpFrom = configuration["QuoteDelivery:Smtp:From"];
        if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpFrom))
        {
            await SendRecoveryEmailAsync(user.Email, resetUrl, configuration, ct);
            return Results.Ok(new { message = genericMessage });
        }

        if (environment.IsDevelopment())
            return Results.Ok(new { message = genericMessage, developmentResetUrl = resetUrl });

        app.Logger.LogWarning("Password recovery requested but SMTP is not configured.");
        return Results.Ok(new { message = genericMessage });
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        ApplicationDbContext db,
        IConfiguration configuration,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Results.BadRequest(new { message = "La contraseña debe tener al menos 8 caracteres." });
        if (request.Password != request.ConfirmPassword)
            return Results.BadRequest(new { message = "Las contraseñas no coinciden." });

        if (!TryReadToken(request.Token, configuration, out var payload))
            return Results.BadRequest(new { message = "El enlace de recuperación es inválido o ha vencido." });

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == payload.UserId && x.IsActive, ct);
        if (user is null || !FixedEquals(payload.PasswordHashFingerprint, Fingerprint(user.PasswordHash)))
            return Results.BadRequest(new { message = "El enlace de recuperación ya no es válido." });

        user.ChangePassword(hasher.Hash(request.Password));
        user.Unlock();
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { message = "Contraseña actualizada correctamente." });
    }

    private static string CreateToken(Guid userId, string passwordHash, DateTime expiresUtc, IConfiguration configuration)
    {
        var payload = new RecoveryTokenPayload(userId, expiresUtc.Ticks, Fingerprint(passwordHash));
        var json = JsonSerializer.Serialize(payload);
        var data = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var signature = Sign(data, configuration);
        return $"{data}.{signature}";
    }

    private static bool TryReadToken(string token, IConfiguration configuration, out RecoveryTokenPayload payload)
    {
        payload = default!;
        if (string.IsNullOrWhiteSpace(token)) return false;
        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !FixedEquals(parts[1], Sign(parts[0], configuration))) return false;
        try
        {
            payload = JsonSerializer.Deserialize<RecoveryTokenPayload>(WebEncoders.Base64UrlDecode(parts[0]))!;
            return payload is not null && new DateTime(payload.ExpiresTicks, DateTimeKind.Utc) > DateTime.UtcNow;
        }
        catch { return false; }
    }

    private static string Sign(string data, IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret no está configurado.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Fingerprint(string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));

    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static async Task SendRecoveryEmailAsync(string email, string resetUrl, IConfiguration configuration, CancellationToken ct)
    {
        var host = configuration["QuoteDelivery:Smtp:Host"]!;
        var port = int.TryParse(configuration["QuoteDelivery:Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(configuration["QuoteDelivery:Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
        var user = configuration["QuoteDelivery:Smtp:User"];
        var password = configuration["QuoteDelivery:Smtp:Password"];
        var from = configuration["QuoteDelivery:Smtp:From"]!;

        using var message = new MailMessage(from, email)
        {
            Subject = "CRM MIDA · Recuperación de contraseña",
            Body = $"Solicitaste restablecer tu contraseña de CRM MIDA.\n\nAbre este enlace (vigente por 30 minutos):\n{resetUrl}\n\nSi no solicitaste este cambio, ignora este mensaje.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, password);
        await client.SendMailAsync(message, ct);
    }

    private sealed record RecoveryTokenPayload(Guid UserId, long ExpiresTicks, string PasswordHashFingerprint);
}

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string Password, string ConfirmPassword);
