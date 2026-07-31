using System.Data;
using System.Security.Claims;
using System.Text.Json;
using CrmMida.Infrastructure.Persistence;

namespace CrmMida.Api.Administration;

public sealed class AuditService(ApplicationDbContext db)
{
    public async Task WriteAsync(
        ClaimsPrincipal user,
        string action,
        string entityType,
        string? entityId,
        object? details,
        CancellationToken ct)
    {
        var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;
        var email = user.FindFirstValue(ClaimTypes.Email);
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO audit_logs
            ("Id", "UserId", "UserEmail", "Action", "EntityType", "EntityId", "DetailsJson", "CreatedAtUtc")
            VALUES (@id, @userId, @email, @action, @entityType, @entityId, @details, @created);
            """;
        Add(command, "@id", Guid.NewGuid());
        Add(command, "@userId", userId.HasValue ? userId.Value : DBNull.Value);
        Add(command, "@email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
        Add(command, "@action", action);
        Add(command, "@entityType", entityType);
        Add(command, "@entityId", string.IsNullOrWhiteSpace(entityId) ? DBNull.Value : entityId);
        Add(command, "@details", details is null ? DBNull.Value : JsonSerializer.Serialize(details));
        Add(command, "@created", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
