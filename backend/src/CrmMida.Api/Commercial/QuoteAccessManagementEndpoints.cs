using System.Data;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class QuoteAccessManagementEndpoints
{
    public static void MapQuoteAccessManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/quotes").RequireAuthorization();

        group.MapGet("/{id:guid}/public-links", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await db.Quotes.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();

            var rows = new List<QuotePublicLinkHistoryDto>();
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "ExpiresAtUtc", "CreatedAtUtc", "OpenedAtUtc", "RespondedAtUtc",
                       "Decision", "DecisionComment", "IsRevoked"
                FROM quote_public_accesses
                WHERE "QuoteId" = @quoteId
                ORDER BY "CreatedAtUtc" DESC;
                """;
            AddParameter(command, "@quoteId", id);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new QuotePublicLinkHistoryDto(
                    reader.GetGuid(0), reader.GetDateTime(1), reader.GetDateTime(2),
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetBoolean(7)));
            }

            return Results.Ok(rows);
        }).RequireAuthorization("quotes.read");

        group.MapPost("/{quoteId:guid}/public-links/{accessId:guid}/revoke", async (
            Guid quoteId,
            Guid accessId,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE quote_public_accesses
                SET "IsRevoked" = true
                WHERE "Id" = @accessId AND "QuoteId" = @quoteId AND "IsRevoked" = false;
                """;
            AddParameter(command, "@accessId", accessId);
            AddParameter(command, "@quoteId", quoteId);
            var affected = await command.ExecuteNonQueryAsync(ct);
            return affected == 0
                ? Results.NotFound(new { message = "El acceso no existe o ya estaba revocado." })
                : Results.Ok(new { status = "revoked", accessId });
        }).RequireAuthorization("quotes.manage");
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

public sealed record QuotePublicLinkHistoryDto(
    Guid Id,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime? OpenedAtUtc,
    DateTime? RespondedAtUtc,
    string? Decision,
    string? DecisionComment,
    bool IsRevoked);
