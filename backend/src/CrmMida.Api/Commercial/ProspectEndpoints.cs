using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class ProspectEndpoints
{
    public static IEndpointRouteBuilder MapProspectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/prospects").WithTags("Prospects");

        group.MapGet("/", GetProspectsAsync).RequireAuthorization("prospects.read");
        group.MapGet("/{id:guid}", GetProspectAsync).RequireAuthorization("prospects.read");
        group.MapPost("/", CreateProspectAsync).RequireAuthorization("prospects.manage");
        group.MapPut("/{id:guid}", UpdateProspectAsync).RequireAuthorization("prospects.manage");
        group.MapDelete("/{id:guid}", DeleteProspectAsync).RequireAuthorization("prospects.manage");
        group.MapPost("/{id:guid}/convert", ConvertProspectAsync).RequireAuthorization("prospects.manage");

        return endpoints;
    }

    private static async Task<IResult> GetProspectsAsync(
        ApplicationDbContext dbContext,
        string? search,
        string? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Prospects.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(term) ||
                (x.CompanyName != null && x.CompanyName.ToLower().Contains(term)) ||
                (x.Email != null && x.Email.ToLower().Contains(term)) ||
                (x.Rfc != null && x.Rfc.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status == normalizedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<ProspectDto>(items, total, page, pageSize));
    }

    private static async Task<IResult> GetProspectAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var prospect = await dbContext.Prospects.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        return prospect is null ? Results.NotFound() : Results.Ok(ToDto(prospect));
    }

    private static async Task<IResult> CreateProspectAsync(
        ProspectRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return validation;

        var prospect = new Prospect(request.Name, request.Source, request.AssignedUserId);
        prospect.Update(
            request.Name,
            request.CompanyName,
            request.Rfc,
            request.Email,
            request.Phone,
            request.Source,
            request.Interest,
            request.Status ?? "new",
            request.Qualification ?? "unrated",
            request.Notes,
            request.AssignedUserId);

        dbContext.Prospects.Add(prospect);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/prospects/{prospect.Id}", ToDto(prospect));
    }

    private static async Task<IResult> UpdateProspectAsync(
        Guid id,
        ProspectRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return validation;

        var prospect = await dbContext.Prospects
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (prospect is null) return Results.NotFound();
        if (prospect.Status == "converted")
            return Results.Conflict(new { message = "Un prospecto convertido ya no puede modificarse." });

        prospect.Update(
            request.Name,
            request.CompanyName,
            request.Rfc,
            request.Email,
            request.Phone,
            request.Source,
            request.Interest,
            request.Status ?? "new",
            request.Qualification ?? "unrated",
            request.Notes,
            request.AssignedUserId);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDto(prospect));
    }

    private static async Task<IResult> DeleteProspectAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var prospect = await dbContext.Prospects
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (prospect is null) return Results.NotFound();
        prospect.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ConvertProspectAsync(
        Guid id,
        ConvertProspectRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var prospect = await dbContext.Prospects
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (prospect is null) return Results.NotFound();
        if (prospect.Status == "converted")
            return Results.Conflict(new { message = "El prospecto ya fue convertido." });

        var tradeName = string.IsNullOrWhiteSpace(request.TradeName)
            ? prospect.CompanyName ?? prospect.Name
            : request.TradeName.Trim();
        var businessName = string.IsNullOrWhiteSpace(request.BusinessName)
            ? prospect.CompanyName ?? prospect.Name
            : request.BusinessName.Trim();
        var rfc = (request.Rfc ?? prospect.Rfc)?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(rfc) || rfc.Length is < 12 or > 13)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["rfc"] = ["Para convertir el prospecto se requiere un RFC válido de 12 o 13 caracteres."]
            });

        if (await dbContext.Companies.AnyAsync(x => x.Rfc == rfc, cancellationToken))
            return Results.Conflict(new { message = "Ya existe una empresa registrada con ese RFC." });

        var company = new Company(tradeName, businessName, rfc, request.CustomerType ?? "prospect", prospect.AssignedUserId);
        company.Update(
            tradeName,
            businessName,
            rfc,
            request.TaxRegime,
            request.FiscalPostalCode,
            prospect.Email,
            prospect.Phone,
            null,
            null,
            null,
            null,
            request.CustomerType ?? "prospect",
            "active",
            "converted-from-prospect",
            null,
            prospect.AssignedUserId);

        dbContext.Companies.Add(company);

        if (!string.IsNullOrWhiteSpace(prospect.Email) || !string.IsNullOrWhiteSpace(prospect.Phone))
        {
            var parts = prospect.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var contact = new Contact(company.Id, parts[0], parts.Length > 1 ? parts[1] : "Contacto", prospect.Email);
            contact.Update(parts[0], parts.Length > 1 ? parts[1] : "Contacto", null, null, prospect.Phone, prospect.Phone, prospect.Email, true, false, false, false, false);
            dbContext.Contacts.Add(contact);
        }

        prospect.MarkConverted(company.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ConvertProspectResult(prospect.Id, company.Id, company.TradeName));
    }

    private static IResult? Validate(ProspectRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["El nombre es obligatorio."];
        if (string.IsNullOrWhiteSpace(request.Source)) errors["source"] = ["El origen es obligatorio."];
        if (!string.IsNullOrWhiteSpace(request.Rfc) && request.Rfc.Trim().Length is < 12 or > 13)
            errors["rfc"] = ["El RFC debe contener 12 o 13 caracteres."];
        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static ProspectDto ToDto(Prospect prospect) => new(
        prospect.Id,
        prospect.Name,
        prospect.CompanyName,
        prospect.Rfc,
        prospect.Email,
        prospect.Phone,
        prospect.Source,
        prospect.Interest,
        prospect.Status,
        prospect.Qualification,
        prospect.Notes,
        prospect.AssignedUserId,
        prospect.ConvertedCompanyId,
        prospect.ConvertedAtUtc,
        prospect.CreatedAtUtc);
}

public sealed record ProspectRequest(
    string Name,
    string Source,
    string? CompanyName,
    string? Rfc,
    string? Email,
    string? Phone,
    string? Interest,
    string? Status,
    string? Qualification,
    string? Notes,
    Guid? AssignedUserId);

public sealed record ConvertProspectRequest(
    string? TradeName,
    string? BusinessName,
    string? Rfc,
    string? CustomerType,
    string? TaxRegime,
    string? FiscalPostalCode);

public sealed record ProspectDto(
    Guid Id,
    string Name,
    string? CompanyName,
    string? Rfc,
    string? Email,
    string? Phone,
    string Source,
    string? Interest,
    string Status,
    string Qualification,
    string? Notes,
    Guid? AssignedUserId,
    Guid? ConvertedCompanyId,
    DateTime? ConvertedAtUtc,
    DateTime CreatedAtUtc);

public sealed record ConvertProspectResult(Guid ProspectId, Guid CompanyId, string CompanyName);
