using System.Data;
using System.Security.Cryptography;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class QuotePortalEndpoints
{
    public static void MapQuotePortalEndpoints(this WebApplication app)
    {
        var secured = app.MapGroup("/api/v1/quotes").RequireAuthorization();

        secured.MapGet("/{id:guid}/deliveries", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = new List<DeliveryHistoryDto>();
            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "Channel", "Recipient", "Status", "ProviderReference", "ErrorMessage", "AttemptNumber", "CreatedAtUtc", "CompletedAtUtc"
                FROM quote_delivery_attempts
                WHERE "QuoteId" = @quoteId
                ORDER BY "CreatedAtUtc" DESC;
                """;
            AddParameter(command, "@quoteId", id);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new DeliveryHistoryDto(
                    reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetInt32(6), reader.GetDateTime(7), reader.IsDBNull(8) ? null : reader.GetDateTime(8)));
            }
            return Results.Ok(rows);
        }).RequireAuthorization("quotes.read");

        secured.MapPost("/{id:guid}/deliveries/send", async (
            Guid id,
            SendQuoteRequest request,
            ApplicationDbContext db,
            QuoteDeliveryService deliveryService,
            CancellationToken ct) =>
        {
            var quote = await db.Quotes.Include(x => x.Company).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return Results.NotFound();
            if (quote.Items.Count == 0) return Results.BadRequest(new { message = "La cotización no tiene partidas." });

            var attemptId = Guid.NewGuid();
            var attemptNumber = await NextAttemptNumberAsync(db, id, request.Channel, request.Recipient, ct);
            await InsertAttemptAsync(db, attemptId, id, request.Channel, request.Recipient, attemptNumber, ct);

            var result = await deliveryService.SendAsync(quote, request.Channel, request.Recipient, request.Message, ct);
            await CompleteAttemptAsync(db, attemptId, result.Status, result.Reference, result.Message, ct);

            if (result.Status == "sent" && quote.Status == "draft")
            {
                quote.MarkSent();
                await db.SaveChangesAsync(ct);
            }

            return result.Status switch
            {
                "sent" => Results.Ok(new { deliveryId = attemptId, attemptNumber, result.Status, result.Message, result.Reference }),
                "not_configured" => Results.Conflict(new { deliveryId = attemptId, attemptNumber, result.Status, result.Message }),
                _ => Results.BadRequest(new { deliveryId = attemptId, attemptNumber, result.Status, result.Message })
            };
        }).RequireAuthorization("quotes.manage");

        secured.MapPost("/{id:guid}/public-link", async (Guid id, CreatePublicLinkRequest request, ApplicationDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!await db.Quotes.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var days = Math.Clamp(request.ValidDays, 1, 60);
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var hash = QuotePublicAccess.HashToken(token);
            var accessId = Guid.NewGuid();
            var expires = DateTime.UtcNow.AddDays(days);

            await using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO quote_public_accesses ("Id", "QuoteId", "TokenHash", "ExpiresAtUtc", "CreatedAtUtc", "IsRevoked")
                VALUES (@id, @quoteId, @hash, @expires, @created, false);
                """;
            AddParameter(command, "@id", accessId);
            AddParameter(command, "@quoteId", id);
            AddParameter(command, "@hash", hash);
            AddParameter(command, "@expires", expires);
            AddParameter(command, "@created", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync(ct);

            var portalUrl = configuration["QuotePortal:PublicUrl"]?.TrimEnd('/') ?? "http://localhost:5173/public/quotes";
            return Results.Ok(new PublicLinkDto(accessId, $"{portalUrl}/{token}", expires));
        }).RequireAuthorization("quotes.manage");

        var publicGroup = app.MapGroup("/api/public/quotes");

        publicGroup.MapGet("/{token}", async (string token, ApplicationDbContext db, CancellationToken ct) =>
        {
            var access = await FindAccessAsync(db, token, ct);
            if (access is null || access.ExpiresAtUtc <= DateTime.UtcNow || access.IsRevoked) return Results.NotFound();

            var quote = await db.Quotes.AsNoTracking().Include(x => x.Company).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == access.QuoteId, ct);
            if (quote is null) return Results.NotFound();
            await RegisterOpenAsync(db, access.Id, ct);

            return Results.Ok(new PublicQuoteDto(
                quote.Folio, quote.Company?.TradeName ?? string.Empty, quote.Title, quote.Currency,
                quote.Subtotal, quote.Tax, quote.Discount, quote.Total, quote.ValidUntilUtc, quote.Status, quote.Notes,
                quote.Items.Select(x => new PublicQuoteItemDto(x.Description, x.Quantity, x.UnitPrice, x.TaxRate, x.Total)).ToArray(),
                access.ExpiresAtUtc, access.Decision));
        });

        publicGroup.MapGet("/{token}/pdf", async (string token, ApplicationDbContext db, QuotePdfService pdfService, CancellationToken ct) =>
        {
            var access = await FindAccessAsync(db, token, ct);
            if (access is null || access.ExpiresAtUtc <= DateTime.UtcNow || access.IsRevoked) return Results.NotFound();
            var quote = await db.Quotes.AsNoTracking().Include(x => x.Company).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == access.QuoteId, ct);
            return quote is null ? Results.NotFound() : Results.File(pdfService.Generate(quote), "application/pdf", $"{quote.Folio}.pdf");
        });

        publicGroup.MapPost("/{token}/decision", async (string token, PublicDecisionRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var decision = request.Decision.Trim().ToLowerInvariant();
            if (decision is not ("accepted" or "rejected")) return Results.BadRequest(new { message = "La decisión debe ser accepted o rejected." });

            var access = await FindAccessAsync(db, token, ct);
            if (access is null || access.ExpiresAtUtc <= DateTime.UtcNow || access.IsRevoked || access.Decision is not null) return Results.Conflict(new { message = "El enlace ya no está disponible." });

            var quote = await db.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == access.QuoteId, ct);
            if (quote is null) return Results.NotFound();
            if (decision == "accepted") quote.MarkAccepted(); else quote.MarkRejected();
            await db.SaveChangesAsync(ct);

            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE quote_public_accesses
                SET "Decision" = @decision, "DecisionComment" = @comment, "RespondedAtUtc" = @responded
                WHERE "Id" = @id;
                """;
            AddParameter(command, "@decision", decision);
            AddParameter(command, "@comment", string.IsNullOrWhiteSpace(request.Comment) ? DBNull.Value : request.Comment.Trim());
            AddParameter(command, "@responded", DateTime.UtcNow);
            AddParameter(command, "@id", access.Id);
            await command.ExecuteNonQueryAsync(ct);

            return Results.Ok(new { status = decision, quote.Folio });
        });
    }

    private static async Task<int> NextAttemptNumberAsync(ApplicationDbContext db, Guid quoteId, string channel, string recipient, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(\"AttemptNumber\"), 0) + 1 FROM quote_delivery_attempts WHERE \"QuoteId\"=@quoteId AND \"Channel\"=@channel AND \"Recipient\"=@recipient;";
        AddParameter(command, "@quoteId", quoteId);
        AddParameter(command, "@channel", channel.Trim().ToLowerInvariant());
        AddParameter(command, "@recipient", recipient.Trim());
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static async Task InsertAttemptAsync(ApplicationDbContext db, Guid id, Guid quoteId, string channel, string recipient, int number, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO quote_delivery_attempts (\"Id\",\"QuoteId\",\"Channel\",\"Recipient\",\"Status\",\"AttemptNumber\",\"CreatedAtUtc\") VALUES (@id,@quoteId,@channel,@recipient,'pending',@number,@created);";
        AddParameter(command, "@id", id); AddParameter(command, "@quoteId", quoteId); AddParameter(command, "@channel", channel.Trim().ToLowerInvariant()); AddParameter(command, "@recipient", recipient.Trim()); AddParameter(command, "@number", number); AddParameter(command, "@created", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task CompleteAttemptAsync(ApplicationDbContext db, Guid id, string status, string? reference, string message, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE quote_delivery_attempts SET \"Status\"=@status, \"ProviderReference\"=@reference, \"ErrorMessage\"=@error, \"CompletedAtUtc\"=@completed WHERE \"Id\"=@id;";
        AddParameter(command, "@status", status == "sent" ? "sent" : "failed"); AddParameter(command, "@reference", string.IsNullOrWhiteSpace(reference) ? DBNull.Value : reference); AddParameter(command, "@error", status == "sent" ? DBNull.Value : message); AddParameter(command, "@completed", DateTime.UtcNow); AddParameter(command, "@id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<AccessRow?> FindAccessAsync(ApplicationDbContext db, string token, CancellationToken ct)
    {
        var hash = QuotePublicAccess.HashToken(token);
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\",\"QuoteId\",\"ExpiresAtUtc\",\"IsRevoked\",\"Decision\" FROM quote_public_accesses WHERE \"TokenHash\"=@hash LIMIT 1;";
        AddParameter(command, "@hash", hash);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new AccessRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2), reader.GetBoolean(3), reader.IsDBNull(4) ? null : reader.GetString(4)) : null;
    }

    private static async Task RegisterOpenAsync(ApplicationDbContext db, Guid id, CancellationToken ct)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE quote_public_accesses SET \"OpenedAtUtc\"=COALESCE(\"OpenedAtUtc\", @opened) WHERE \"Id\"=@id;";
        AddParameter(command, "@opened", DateTime.UtcNow); AddParameter(command, "@id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }

    private sealed record AccessRow(Guid Id, Guid QuoteId, DateTime ExpiresAtUtc, bool IsRevoked, string? Decision);
}

public sealed record CreatePublicLinkRequest(int ValidDays = 15);
public sealed record PublicDecisionRequest(string Decision, string? Comment);
public sealed record PublicLinkDto(Guid Id, string Url, DateTime ExpiresAtUtc);
public sealed record DeliveryHistoryDto(Guid Id, string Channel, string Recipient, string Status, string? ProviderReference, string? ErrorMessage, int AttemptNumber, DateTime CreatedAtUtc, DateTime? CompletedAtUtc);
public sealed record PublicQuoteDto(string Folio, string CompanyName, string Title, string Currency, decimal Subtotal, decimal Tax, decimal Discount, decimal Total, DateTime ValidUntilUtc, string Status, string? Notes, IReadOnlyCollection<PublicQuoteItemDto> Items, DateTime LinkExpiresAtUtc, string? Decision);
public sealed record PublicQuoteItemDto(string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, decimal Total);
