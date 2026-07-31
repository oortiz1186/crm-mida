using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class CommercialEndpoints
{
    public static IEndpointRouteBuilder MapCommercialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var companies = endpoints.MapGroup("/api/v1/companies")
            .RequireAuthorization()
            .WithTags("Companies");

        companies.MapGet("/", GetCompaniesAsync);
        companies.MapGet("/{id:guid}", GetCompanyAsync);
        companies.MapPost("/", CreateCompanyAsync);
        companies.MapPut("/{id:guid}", UpdateCompanyAsync);
        companies.MapDelete("/{id:guid}", DeleteCompanyAsync);
        companies.MapGet("/{companyId:guid}/contacts", GetContactsAsync);
        companies.MapPost("/{companyId:guid}/contacts", CreateContactAsync);

        var contacts = endpoints.MapGroup("/api/v1/contacts")
            .RequireAuthorization()
            .WithTags("Contacts");

        contacts.MapPut("/{id:guid}", UpdateContactAsync);
        contacts.MapDelete("/{id:guid}", DeleteContactAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCompaniesAsync(
        ApplicationDbContext dbContext,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Companies
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.TradeName.ToLower().Contains(term) ||
                x.BusinessName.ToLower().Contains(term) ||
                x.Rfc.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.TradeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CompanyListItemDto(
                x.Id,
                x.TradeName,
                x.BusinessName,
                x.Rfc,
                x.CustomerType,
                x.Status,
                x.Email,
                x.Phone,
                x.Contacts.Count(c => c.IsActive)))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<CompanyListItemDto>(items, total, page, pageSize));
    }

    private static async Task<IResult> GetCompanyAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .AsNoTracking()
            .Include(x => x.Contacts.Where(c => c.IsActive))
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        return company is null
            ? Results.NotFound()
            : Results.Ok(ToDto(company));
    }

    private static async Task<IResult> CreateCompanyAsync(
        CompanyRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCompany(request);
        if (validation is not null)
        {
            return validation;
        }

        var rfc = request.Rfc.Trim().ToUpperInvariant();
        if (await dbContext.Companies.AnyAsync(x => x.Rfc == rfc, cancellationToken))
        {
            return Results.Conflict(new { message = "Ya existe una empresa registrada con ese RFC." });
        }

        var company = new Company(
            request.TradeName,
            request.BusinessName,
            rfc,
            request.CustomerType,
            request.AssignedUserId);

        company.Update(
            request.TradeName,
            request.BusinessName,
            rfc,
            request.TaxRegime,
            request.FiscalPostalCode,
            request.Email,
            request.Phone,
            request.Website,
            request.Address,
            request.City,
            request.State,
            request.CustomerType,
            request.Status ?? "active",
            request.Tags,
            request.ExternalContpaqiId,
            request.AssignedUserId);

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/companies/{company.Id}", ToDto(company));
    }

    private static async Task<IResult> UpdateCompanyAsync(
        Guid id,
        CompanyRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCompany(request);
        if (validation is not null)
        {
            return validation;
        }

        var company = await dbContext.Companies
            .Include(x => x.Contacts)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (company is null)
        {
            return Results.NotFound();
        }

        var rfc = request.Rfc.Trim().ToUpperInvariant();
        if (await dbContext.Companies.AnyAsync(x => x.Id != id && x.Rfc == rfc, cancellationToken))
        {
            return Results.Conflict(new { message = "Ya existe otra empresa registrada con ese RFC." });
        }

        company.Update(
            request.TradeName,
            request.BusinessName,
            rfc,
            request.TaxRegime,
            request.FiscalPostalCode,
            request.Email,
            request.Phone,
            request.Website,
            request.Address,
            request.City,
            request.State,
            request.CustomerType,
            request.Status ?? "active",
            request.Tags,
            request.ExternalContpaqiId,
            request.AssignedUserId);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDto(company));
    }

    private static async Task<IResult> DeleteCompanyAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.SingleOrDefaultAsync(
            x => x.Id == id && x.IsActive,
            cancellationToken);

        if (company is null)
        {
            return Results.NotFound();
        }

        company.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetContactsAsync(
        Guid companyId,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Companies.AnyAsync(
            x => x.Id == companyId && x.IsActive,
            cancellationToken);

        if (!exists)
        {
            return Results.NotFound();
        }

        var contacts = await dbContext.Contacts
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.FirstName)
            .Select(x => ToContactDto(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(contacts);
    }

    private static async Task<IResult> CreateContactAsync(
        Guid companyId,
        ContactRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies
            .Include(x => x.Contacts)
            .SingleOrDefaultAsync(x => x.Id == companyId && x.IsActive, cancellationToken);

        if (company is null)
        {
            return Results.NotFound();
        }

        var validation = ValidateContact(request);
        if (validation is not null)
        {
            return validation;
        }

        if (request.IsPrimary)
        {
            foreach (var currentPrimary in company.Contacts.Where(x => x.IsActive && x.IsPrimary))
            {
                currentPrimary.SetPrimary(false);
            }
        }

        var contact = new Contact(companyId, request.FirstName, request.LastName, request.Email);
        contact.Update(
            request.FirstName,
            request.LastName,
            request.Position,
            request.Area,
            request.Phone,
            request.Mobile,
            request.Email,
            request.IsPrimary,
            request.IsPurchasingContact,
            request.IsTechnicalContact,
            request.IsBillingContact,
            request.MarketingConsent);

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/contacts/{contact.Id}", ToContactDto(contact));
    }

    private static async Task<IResult> UpdateContactAsync(
        Guid id,
        ContactRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validation = ValidateContact(request);
        if (validation is not null)
        {
            return validation;
        }

        var contact = await dbContext.Contacts
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (contact is null)
        {
            return Results.NotFound();
        }

        if (request.IsPrimary)
        {
            var currentPrimaries = await dbContext.Contacts
                .Where(x => x.CompanyId == contact.CompanyId && x.Id != id && x.IsActive && x.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var currentPrimary in currentPrimaries)
            {
                currentPrimary.SetPrimary(false);
            }
        }

        contact.Update(
            request.FirstName,
            request.LastName,
            request.Position,
            request.Area,
            request.Phone,
            request.Mobile,
            request.Email,
            request.IsPrimary,
            request.IsPurchasingContact,
            request.IsTechnicalContact,
            request.IsBillingContact,
            request.MarketingConsent);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToContactDto(contact));
    }

    private static async Task<IResult> DeleteContactAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts.SingleOrDefaultAsync(
            x => x.Id == id && x.IsActive,
            cancellationToken);

        if (contact is null)
        {
            return Results.NotFound();
        }

        contact.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static IResult? ValidateCompany(CompanyRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.TradeName))
            errors["tradeName"] = ["El nombre comercial es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.BusinessName))
            errors["businessName"] = ["La razón social es obligatoria."];
        if (string.IsNullOrWhiteSpace(request.Rfc) || request.Rfc.Trim().Length is < 12 or > 13)
            errors["rfc"] = ["El RFC debe contener 12 o 13 caracteres."];
        if (string.IsNullOrWhiteSpace(request.CustomerType))
            errors["customerType"] = ["El tipo de cliente es obligatorio."];

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult? ValidateContact(ContactRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["El nombre es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["El apellido es obligatorio."];

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static CompanyDto ToDto(Company company) => new(
        company.Id,
        company.TradeName,
        company.BusinessName,
        company.Rfc,
        company.TaxRegime,
        company.FiscalPostalCode,
        company.Email,
        company.Phone,
        company.Website,
        company.Address,
        company.City,
        company.State,
        company.CustomerType,
        company.Status,
        company.Tags,
        company.ExternalContpaqiId,
        company.AssignedUserId,
        company.Contacts.Where(x => x.IsActive).Select(ToContactDto).ToArray());

    private static ContactDto ToContactDto(Contact contact) => new(
        contact.Id,
        contact.CompanyId,
        contact.FirstName,
        contact.LastName,
        contact.Position,
        contact.Area,
        contact.Phone,
        contact.Mobile,
        contact.Email,
        contact.IsPrimary,
        contact.IsPurchasingContact,
        contact.IsTechnicalContact,
        contact.IsBillingContact,
        contact.MarketingConsent);
}

public sealed record CompanyRequest(
    string TradeName,
    string BusinessName,
    string Rfc,
    string CustomerType,
    string? TaxRegime,
    string? FiscalPostalCode,
    string? Email,
    string? Phone,
    string? Website,
    string? Address,
    string? City,
    string? State,
    string? Status,
    string? Tags,
    string? ExternalContpaqiId,
    Guid? AssignedUserId);

public sealed record ContactRequest(
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

public sealed record CompanyListItemDto(
    Guid Id,
    string TradeName,
    string BusinessName,
    string Rfc,
    string CustomerType,
    string Status,
    string? Email,
    string? Phone,
    int ContactsCount);

public sealed record CompanyDto(
    Guid Id,
    string TradeName,
    string BusinessName,
    string Rfc,
    string? TaxRegime,
    string? FiscalPostalCode,
    string? Email,
    string? Phone,
    string? Website,
    string? Address,
    string? City,
    string? State,
    string CustomerType,
    string Status,
    string? Tags,
    string? ExternalContpaqiId,
    Guid? AssignedUserId,
    IReadOnlyCollection<ContactDto> Contacts);

public sealed record ContactDto(
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

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Total,
    int Page,
    int PageSize);
