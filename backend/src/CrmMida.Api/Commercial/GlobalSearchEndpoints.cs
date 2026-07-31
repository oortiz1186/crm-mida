using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class GlobalSearchEndpoints
{
    public static void MapGlobalSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/search", async (
            string q,
            int? limit,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var term = q?.Trim();
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Results.BadRequest(new { message = "Escribe al menos dos caracteres." });

            var take = Math.Clamp(limit ?? 8, 1, 20);
            var pattern = $"%{term}%";

            var companies = await db.Companies.AsNoTracking()
                .Where(x => x.IsActive &&
                    (EF.Functions.ILike(x.TradeName, pattern) ||
                     EF.Functions.ILike(x.BusinessName, pattern) ||
                     EF.Functions.ILike(x.Rfc, pattern)))
                .OrderBy(x => x.TradeName)
                .Take(take)
                .Select(x => new SearchResult("company", x.Id, x.TradeName, $"{x.BusinessName} · {x.Rfc}", $"/customers?companyId={x.Id}"))
                .ToListAsync(ct);

            var contacts = await db.Contacts.AsNoTracking()
                .Where(x => x.IsActive &&
                    (EF.Functions.ILike(x.FirstName, pattern) ||
                     EF.Functions.ILike(x.LastName, pattern) ||
                     (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                     (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)) ||
                     (x.Mobile != null && EF.Functions.ILike(x.Mobile, pattern))))
                .OrderBy(x => x.FirstName)
                .Take(take)
                .Select(x => new SearchResult("contact", x.Id, (x.FirstName + " " + x.LastName).Trim(), x.Email ?? x.Mobile ?? x.Phone ?? "Contacto", $"/customers?companyId={x.CompanyId}"))
                .ToListAsync(ct);

            var prospects = await db.Prospects.AsNoTracking()
                .Where(x => x.IsActive &&
                    (EF.Functions.ILike(x.Name, pattern) ||
                     (x.CompanyName != null && EF.Functions.ILike(x.CompanyName, pattern)) ||
                     (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                     (x.Rfc != null && EF.Functions.ILike(x.Rfc, pattern))))
                .OrderBy(x => x.Name)
                .Take(take)
                .Select(x => new SearchResult("prospect", x.Id, x.Name, x.CompanyName ?? x.Status, "/prospects"))
                .ToListAsync(ct);

            var opportunities = await db.Opportunities.AsNoTracking()
                .Where(x => x.IsActive &&
                    (EF.Functions.ILike(x.Name, pattern) ||
                     (x.ProductOrService != null && EF.Functions.ILike(x.ProductOrService, pattern))))
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Take(take)
                .Select(x => new SearchResult("opportunity", x.Id, x.Name, $"{x.Stage} · {x.Probability}%", "/opportunities"))
                .ToListAsync(ct);

            var quotes = await db.Quotes.AsNoTracking()
                .Where(x => EF.Functions.ILike(x.Folio, pattern) || EF.Functions.ILike(x.Title, pattern))
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .Select(x => new SearchResult("quote", x.Id, x.Folio, $"{x.Title} · {x.Status}", "/quotes"))
                .ToListAsync(ct);

            var catalog = await db.CatalogItems.AsNoTracking()
                .Where(x => x.IsActive &&
                    (EF.Functions.ILike(x.Code, pattern) || EF.Functions.ILike(x.Name, pattern)))
                .OrderBy(x => x.Name)
                .Take(take)
                .Select(x => new SearchResult("catalog", x.Id, x.Name, $"{x.Code} · {x.Type}", "/catalog"))
                .ToListAsync(ct);

            return Results.Ok(new
            {
                query = term,
                total = companies.Count + contacts.Count + prospects.Count + opportunities.Count + quotes.Count + catalog.Count,
                groups = new[]
                {
                    new SearchGroup("Empresas", companies),
                    new SearchGroup("Contactos", contacts),
                    new SearchGroup("Prospectos", prospects),
                    new SearchGroup("Oportunidades", opportunities),
                    new SearchGroup("Cotizaciones", quotes),
                    new SearchGroup("Catálogo", catalog)
                }.Where(x => x.Items.Count > 0)
            });
        }).RequireAuthorization();
    }

    private sealed record SearchResult(string Type, Guid Id, string Title, string Subtitle, string Url);
    private sealed record SearchGroup(string Label, IReadOnlyCollection<SearchResult> Items);
}
