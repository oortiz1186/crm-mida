using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/catalog").RequireAuthorization();

        group.MapGet("/", async (string? search, string? type, ApplicationDbContext db, CancellationToken ct) =>
        {
            var query = db.CatalogItems.AsNoTracking().Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type.Trim().ToLowerInvariant());
            return Results.Ok(await query.OrderBy(x => x.Name).Select(x => new CatalogItemDto(x.Id, x.Code, x.Name, x.Type, x.Description, x.UnitPrice, x.TaxRate, x.IsActive)).ToListAsync(ct));
        }).RequireAuthorization("catalog.read");

        group.MapPost("/", async (SaveCatalogItemRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var code = request.Code.Trim().ToUpperInvariant();
            if (await db.CatalogItems.AnyAsync(x => x.Code == code, ct)) return Results.Conflict(new { message = "El código ya existe." });
            var item = new CatalogItem(request.Code, request.Name, request.Type, request.UnitPrice, request.TaxRate);
            item.Update(request.Code, request.Name, request.Type, request.UnitPrice, request.TaxRate, request.Description);
            db.CatalogItems.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/catalog/{item.Id}", new CatalogItemDto(item.Id, item.Code, item.Name, item.Type, item.Description, item.UnitPrice, item.TaxRate, item.IsActive));
        }).RequireAuthorization("catalog.manage");

        group.MapPut("/{id:guid}", async (Guid id, SaveCatalogItemRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var item = await db.CatalogItems.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return Results.NotFound();
            var code = request.Code.Trim().ToUpperInvariant();
            if (await db.CatalogItems.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Results.Conflict(new { message = "El código ya existe." });
            item.Update(request.Code, request.Name, request.Type, request.UnitPrice, request.TaxRate, request.Description);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new CatalogItemDto(item.Id, item.Code, item.Name, item.Type, item.Description, item.UnitPrice, item.TaxRate, item.IsActive));
        }).RequireAuthorization("catalog.manage");

        group.MapDelete("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var item = await db.CatalogItems.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return Results.NotFound();
            item.Deactivate();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("catalog.manage");
    }
}

public sealed record SaveCatalogItemRequest(string Code, string Name, string Type, string? Description, decimal UnitPrice, decimal TaxRate);
public sealed record CatalogItemDto(Guid Id, string Code, string Name, string Type, string? Description, decimal UnitPrice, decimal TaxRate, bool IsActive);
