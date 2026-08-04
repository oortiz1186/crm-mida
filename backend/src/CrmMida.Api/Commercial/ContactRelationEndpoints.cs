using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class ContactRelationEndpoints
{
    public static IEndpointRouteBuilder MapContactRelationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var contacts = endpoints.MapGroup("/api/v1/contact-relations")
            .RequireAuthorization()
            .WithTags("Contact relations");

        contacts.MapGet("/search", SearchContactsAsync);
        contacts.MapGet("/companies/{companyId:guid}/contacts", GetCompanyContactsAsync);
        contacts.MapPost("/companies/{companyId:guid}/contacts/{contactId:guid}", LinkContactAsync);
        contacts.MapDelete("/companies/{companyId:guid}/contacts/{contactId:guid}", UnlinkContactAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCompanyContactsAsync(
        Guid companyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var companyExists = await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, ct);
        if (!companyExists) return Results.NotFound();

        var items = await db.Set<CompanyContact>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Active && x.Contact.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Contact.FirstName)
            .ThenBy(x => x.Contact.LastName)
            .Select(x => new CompanyRelatedContact(
                x.Contact.Id,
                companyId,
                x.Contact.FirstName,
                x.Contact.LastName,
                x.Contact.Position,
                x.Contact.Area,
                x.Contact.Phone,
                x.Contact.Mobile,
                x.Contact.Email,
                x.IsPrimary,
                x.Contact.IsPurchasingContact,
                x.Contact.IsTechnicalContact,
                x.Contact.IsBillingContact,
                x.Contact.MarketingConsent))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> SearchContactsAsync(
        string? search,
        Guid? excludeCompanyId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var term = search?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
            return Results.Ok(Array.Empty<ContactSearchItem>());

        var normalized = term.ToLower();
        var query = db.Contacts
            .AsNoTracking()
            .Where(x => x.IsActive &&
                (x.FirstName.ToLower().Contains(normalized) ||
                 x.LastName.ToLower().Contains(normalized) ||
                 (x.Email != null && x.Email.ToLower().Contains(normalized)) ||
                 (x.Phone != null && x.Phone.Contains(term)) ||
                 (x.Mobile != null && x.Mobile.Contains(term))));

        if (excludeCompanyId.HasValue)
        {
            var companyId = excludeCompanyId.Value;
            query = query.Where(x => !db.Set<CompanyContact>()
                .Any(r => r.CompanyId == companyId && r.ContactId == x.Id && r.Active));
        }

        var items = await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(20)
            .Select(x => new ContactSearchItem(
                x.Id,
                x.FirstName,
                x.LastName,
                x.Email,
                x.Phone,
                x.Mobile,
                x.Position))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> LinkContactAsync(
        Guid companyId,
        Guid contactId,
        ContactRelationRequest request,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var companyExists = await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, ct);
        var contactExists = await db.Contacts.AnyAsync(x => x.Id == contactId && x.IsActive, ct);
        if (!companyExists || !contactExists) return Results.NotFound();

        var relations = db.Set<CompanyContact>();
        if (request.IsPrimary)
        {
            var primaries = await relations
                .Where(x => x.CompanyId == companyId && x.IsPrimary && x.Active && x.ContactId != contactId)
                .ToListAsync(ct);
            foreach (var primary in primaries) primary.Update(false, true);
        }

        var relation = await relations.SingleOrDefaultAsync(
            x => x.CompanyId == companyId && x.ContactId == contactId, ct);

        if (relation is null)
            relations.Add(new CompanyContact(companyId, contactId, request.IsPrimary));
        else
            relation.Update(request.IsPrimary, true);

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> UnlinkContactAsync(
        Guid companyId,
        Guid contactId,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var relation = await db.Set<CompanyContact>().SingleOrDefaultAsync(
            x => x.CompanyId == companyId && x.ContactId == contactId && x.Active, ct);
        if (relation is null) return Results.NotFound();

        relation.Update(false, false);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

public sealed record ContactRelationRequest(bool IsPrimary);
public sealed record ContactSearchItem(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Mobile,
    string? Position);
public sealed record CompanyRelatedContact(
    Guid Id,
    Guid CompanyId,
    string FirstName,
    string LastName,
    string? Position,
    string? Area,
    string? Phone,
    string? Mobile,
    string? Email,
    bool IsPrimary,
    bool IsPurchasingContact,
    bool IsTechnicalContact,
    bool IsBillingContact,
    bool MarketingConsent);
