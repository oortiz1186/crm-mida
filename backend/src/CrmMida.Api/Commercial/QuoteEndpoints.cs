using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/quotes").RequireAuthorization();

        group.MapGet("/", async (string? search, string? status, ApplicationDbContext db, CancellationToken ct) =>
        {
            var query = db.Quotes.AsNoTracking().Include(x => x.Company).Include(x => x.Items).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x => x.Folio.ToLower().Contains(term) || x.Title.ToLower().Contains(term) || x.Company!.TradeName.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToLower());
            var entities = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
            return Results.Ok(entities.Select(ToDto).ToArray());
        }).RequireAuthorization("quotes.read");

        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var quote = await db.Quotes.AsNoTracking().Include(x => x.Company).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
            return quote is null ? Results.NotFound() : Results.Ok(ToDto(quote));
        }).RequireAuthorization("quotes.read");

        group.MapGet("/{id:guid}/pdf", async (Guid id, ApplicationDbContext db, QuotePdfService pdfService, CancellationToken ct) =>
        {
            var quote = await db.Quotes.AsNoTracking().Include(x => x.Company).Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return Results.NotFound();
            var bytes = pdfService.Generate(quote);
            return Results.File(bytes, "application/pdf", $"{quote.Folio}.pdf");
        }).RequireAuthorization("quotes.read");

        group.MapPost("/{id:guid}/send", async (
            Guid id,
            SendQuoteRequest request,
            ApplicationDbContext db,
            QuoteDeliveryService deliveryService,
            CancellationToken ct) =>
        {
            var quote = await db.Quotes
                .Include(x => x.Company)
                .Include(x => x.Items)
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (quote is null) return Results.NotFound();
            if (quote.Items.Count == 0) return Results.BadRequest(new { message = "La cotización no tiene partidas." });

            var result = await deliveryService.SendAsync(quote, request.Channel, request.Recipient, request.Message, ct);
            if (result.Status == "sent" && quote.Status == "draft")
            {
                quote.MarkSent();
                await db.SaveChangesAsync(ct);
            }

            return result.Status switch
            {
                "sent" => Results.Ok(result),
                "not_configured" => Results.Conflict(result),
                _ => Results.BadRequest(result)
            };
        }).RequireAuthorization("quotes.manage");

        group.MapPost("/", async (SaveQuoteRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await db.Companies.AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct)) return Results.BadRequest(new { message = "La empresa no existe." });
            if (request.ContactId.HasValue && !await db.Contacts.AnyAsync(x => x.Id == request.ContactId && x.CompanyId == request.CompanyId && x.IsActive, ct)) return Results.BadRequest(new { message = "El contacto no pertenece a la empresa." });
            if (request.Items.Count == 0) return Results.BadRequest(new { message = "Agrega al menos una partida." });

            var quote = new Quote(request.CompanyId, request.Title, request.ValidUntilUtc, request.OpportunityId, request.ContactId);
            quote.SetFolio(await NextFolioAsync(db, ct));
            quote.Update(request.Title, request.ValidUntilUtc, request.Currency, request.Discount, request.Notes, request.ContactId, request.OpportunityId);
            foreach (var item in request.Items) quote.AddItem(item.Description, item.Quantity, item.UnitPrice, item.TaxRate);

            db.Quotes.Add(quote);
            await db.SaveChangesAsync(ct);
            await db.Entry(quote).Reference(x => x.Company).LoadAsync(ct);
            return Results.Created($"/api/v1/quotes/{quote.Id}", ToDto(quote));
        }).RequireAuthorization("quotes.manage");

        group.MapPut("/{id:guid}", async (Guid id, SaveQuoteRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var quote = await db.Quotes.Include(x => x.Items).Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return Results.NotFound();
            if (request.Items.Count == 0) return Results.BadRequest(new { message = "Agrega al menos una partida." });

            quote.Update(request.Title, request.ValidUntilUtc, request.Currency, request.Discount, request.Notes, request.ContactId, request.OpportunityId);
            foreach (var itemId in quote.Items.Select(x => x.Id).ToArray()) quote.RemoveItem(itemId);
            foreach (var item in request.Items) quote.AddItem(item.Description, item.Quantity, item.UnitPrice, item.TaxRate);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(quote));
        }).RequireAuthorization("quotes.manage");

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeQuoteStatusRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var quote = await db.Quotes.Include(x => x.Items).Include(x => x.Company).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (quote is null) return Results.NotFound();

            switch (request.Status.Trim().ToLowerInvariant())
            {
                case "sent": quote.MarkSent(); break;
                case "accepted": quote.MarkAccepted(); break;
                case "rejected": quote.MarkRejected(); break;
                case "cancelled": quote.Cancel(); break;
                default: return Results.BadRequest(new { message = "Estado no válido." });
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(quote));
        }).RequireAuthorization("quotes.manage");
    }

    private static async Task<string> NextFolioAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Quotes.CountAsync(x => x.CreatedAtUtc.Year == year, ct) + 1;
        return $"COT-{year}-{count:000000}";
    }

    private static QuoteDto ToDto(Quote x) => new(
        x.Id, x.Folio, x.CompanyId, x.Company?.TradeName ?? string.Empty, x.ContactId, x.OpportunityId,
        x.Title, x.Currency, x.Discount, x.Subtotal, x.Tax, x.Total, x.ValidUntilUtc, x.Status, x.Notes,
        x.Items.Select(i => new QuoteItemDto(i.Id, i.Description, i.Quantity, i.UnitPrice, i.TaxRate, i.Subtotal, i.Tax, i.Total)).ToArray());
}

public sealed record SaveQuoteRequest(Guid CompanyId, Guid? ContactId, Guid? OpportunityId, string Title, string Currency, decimal Discount, DateTime ValidUntilUtc, string? Notes, IReadOnlyCollection<SaveQuoteItemRequest> Items);
public sealed record SaveQuoteItemRequest(string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate);
public sealed record ChangeQuoteStatusRequest(string Status);
public sealed record SendQuoteRequest(string Channel, string Recipient, string? Message);
public sealed record QuoteDto(Guid Id, string Folio, Guid CompanyId, string CompanyName, Guid? ContactId, Guid? OpportunityId, string Title, string Currency, decimal Discount, decimal Subtotal, decimal Tax, decimal Total, DateTime ValidUntilUtc, string Status, string? Notes, IReadOnlyCollection<QuoteItemDto> Items);
public sealed record QuoteItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal TaxRate, decimal Subtotal, decimal Tax, decimal Total);
