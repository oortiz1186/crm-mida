using System.Data;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Configuration;

public sealed class SmtpSettingsService(ApplicationDbContext db, IConfiguration configuration)
{
    public async Task<SmtpSettings> GetAsync(CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Host\", \"Port\", \"EnableSsl\", \"UserName\", \"Password\", \"FromEmail\", \"FromName\" FROM smtp_settings WHERE \"Id\" = 1";
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new SmtpSettings(
                reader.GetString(0), reader.GetInt32(1), reader.GetBoolean(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6));
        }

        var host = configuration["QuoteDelivery:Smtp:Host"] ?? string.Empty;
        var port = int.TryParse(configuration["QuoteDelivery:Smtp:Port"], out var parsedPort) ? parsedPort : 587;
        var enableSsl = !bool.TryParse(configuration["QuoteDelivery:Smtp:EnableSsl"], out var parsedSsl) || parsedSsl;
        return new SmtpSettings(host, port, enableSsl,
            configuration["QuoteDelivery:Smtp:User"], configuration["QuoteDelivery:Smtp:Password"],
            configuration["QuoteDelivery:Smtp:From"] ?? string.Empty, null);
    }

    public async Task SaveAsync(SmtpSettings settings, bool preservePassword, CancellationToken ct = default)
    {
        var current = preservePassword ? await GetAsync(ct) : null;
        var password = preservePassword && string.IsNullOrWhiteSpace(settings.Password) ? current?.Password : settings.Password;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO smtp_settings (\"Id\", \"Host\", \"Port\", \"EnableSsl\", \"UserName\", \"Password\", \"FromEmail\", \"FromName\", \"UpdatedAtUtc\")
            VALUES (1, {settings.Host}, {settings.Port}, {settings.EnableSsl}, {settings.UserName}, {password}, {settings.FromEmail}, {settings.FromName}, {DateTime.UtcNow})
            ON CONFLICT (\"Id\") DO UPDATE SET
                \"Host\" = EXCLUDED.\"Host\", \"Port\" = EXCLUDED.\"Port\", \"EnableSsl\" = EXCLUDED.\"EnableSsl\",
                \"UserName\" = EXCLUDED.\"UserName\", \"Password\" = EXCLUDED.\"Password\", \"FromEmail\" = EXCLUDED.\"FromEmail\",
                \"FromName\" = EXCLUDED.\"FromName\", \"UpdatedAtUtc\" = EXCLUDED.\"UpdatedAtUtc\";", ct);
    }
}

public sealed record SmtpSettings(string Host, int Port, bool EnableSsl, string? UserName, string? Password, string FromEmail, string? FromName)
{
    public bool Configured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromEmail);
}
