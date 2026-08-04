using System.Security.Claims;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Administration;

public static class CommercialPremiumSyncEndpoints
{
    public static void MapCommercialPremiumSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/contpaqi").RequireAuthorization("companies.manage");

        group.MapGet("/status", (IConfiguration configuration) =>
        {
            var options = ReadOptions(configuration);
            return Results.Ok(new
            {
                configured = !string.IsNullOrWhiteSpace(options.ConnectionString),
                database = options.AllowedDatabase,
                readOnly = true,
                model = "companies + contacts + company_contacts"
            });
        });

        group.MapPost("/test", async (IConfiguration configuration, CancellationToken ct) =>
        {
            var options = ReadOptions(configuration);
            var validation = ValidateOptions(options);
            if (validation is not null) return Results.BadRequest(new { message = validation });

            await using var connection = new SqlConnection(options.ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT DB_NAME(),
                       (SELECT COUNT(*) FROM admClientes WHERE CTIPOCLIENTE IN (1,3)),
                       (SELECT COUNT(*) FROM admDomicilios WHERE CTIPOCATALOGO = 6 AND CIDCATALOGO > 0);
                """;
            await using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            return Results.Ok(new
            {
                connected = true,
                database = reader.GetString(0),
                companies = reader.GetInt32(1),
                contacts = reader.GetInt32(2),
                readOnly = true
            });
        });

        group.MapGet("/preview", async (int? limit, IConfiguration configuration, CancellationToken ct) =>
        {
            var options = ReadOptions(configuration);
            var validation = ValidateOptions(options);
            if (validation is not null) return Results.BadRequest(new { message = validation });

            var take = Math.Clamp(limit ?? 20, 1, 100);
            var companies = await ReadCompaniesAsync(options, take, ct);
            var contacts = await ReadContactsAsync(options, take, ct);
            return Results.Ok(new
            {
                totalPreview = companies.Count,
                items = companies,
                contactsPreview = contacts.Count,
                contacts
            });
        });

        group.MapPost("/sync", async (
            IConfiguration configuration,
            ApplicationDbContext db,
            AuditService audit,
            ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var options = ReadOptions(configuration);
            var validation = ValidateOptions(options);
            if (validation is not null) return Results.BadRequest(new { message = validation });

            var sourceCompanies = await ReadCompaniesAsync(options, null, ct);
            var sourceContacts = await ReadContactsAsync(options, null, ct);
            var companyByCustomerId = new Dictionary<int, Company>();
            var created = 0;
            var updated = 0;
            var skipped = 0;
            var contactsCreated = 0;
            var contactsUpdated = 0;
            var relationsCreated = 0;
            var errors = new List<object>();

            foreach (var item in sourceCompanies)
            {
                try
                {
                    var rfc = NormalizeRfc(item.Rfc);
                    if (string.IsNullOrWhiteSpace(item.BusinessName) || string.IsNullOrWhiteSpace(rfc))
                    {
                        skipped++;
                        continue;
                    }

                    var externalId = item.Id.ToString();
                    var company = await db.Companies.SingleOrDefaultAsync(x => x.ExternalContpaqiId == externalId, ct)
                        ?? await db.Companies.SingleOrDefaultAsync(x => x.Rfc == rfc, ct);

                    var tradeName = string.IsNullOrWhiteSpace(item.Code) ? item.BusinessName : item.Code;
                    if (company is null)
                    {
                        company = new Company(tradeName, item.BusinessName, rfc, "client");
                        db.Companies.Add(company);
                        created++;
                    }
                    else updated++;

                    company.Update(
                        tradeName,
                        item.BusinessName,
                        rfc,
                        null,
                        null,
                        FirstEmail(item.Email1),
                        null,
                        null,
                        null,
                        null,
                        null,
                        "client",
                        "active",
                        "CONTPAQi",
                        externalId,
                        company.AssignedUserId);

                    await db.SaveChangesAsync(ct);
                    companyByCustomerId[item.Id] = company;
                }
                catch (Exception ex)
                {
                    errors.Add(new { item.Id, item.BusinessName, error = ex.Message });
                }
            }

            foreach (var item in sourceContacts)
            {
                try
                {
                    if (!companyByCustomerId.TryGetValue(item.CustomerId, out var company))
                    {
                        var externalId = item.CustomerId.ToString();
                        company = await db.Companies.SingleOrDefaultAsync(x => x.ExternalContpaqiId == externalId, ct);
                        if (company is null) { skipped++; continue; }
                        companyByCustomerId[item.CustomerId] = company;
                    }

                    var contact = await db.Contacts.SingleOrDefaultAsync(x =>
                        x.ContpaqiDatabase == options.AllowedDatabase &&
                        x.ContpaqiAddressId == item.AddressId, ct);

                    var name = string.IsNullOrWhiteSpace(item.Name)
                        ? $"Contacto CONTPAQi {item.AddressId}"
                        : item.Name;
                    var phone = !string.IsNullOrWhiteSpace(item.Phone1) ? item.Phone1 : item.Phone2;

                    if (contact is null)
                    {
                        var parts = SplitName(name);
                        contact = new Contact(company.Id, parts.FirstName, parts.LastName, FirstEmail(item.Email));
                        db.Contacts.Add(contact);
                        contactsCreated++;
                    }
                    else contactsUpdated++;

                    contact.UpdateFromContpaqi(
                        name,
                        item.Email,
                        null,
                        null,
                        phone,
                        item.CustomerId,
                        item.AddressId,
                        options.AllowedDatabase);

                    await db.SaveChangesAsync(ct);
                    relationsCreated += await EnsureRelationAsync(db, company.Id, contact.Id, false, ct);
                }
                catch (Exception ex)
                {
                    errors.Add(new { item.AddressId, item.CustomerId, error = ex.Message });
                }
            }

            await audit.WriteAsync(principal, "sync", "CommercialPremium", null,
                new
                {
                    companiesRead = sourceCompanies.Count,
                    contactsRead = sourceContacts.Count,
                    created,
                    updated,
                    skipped,
                    contactsCreated,
                    contactsUpdated,
                    relationsCreated,
                    errors = errors.Count
                }, ct);

            return Results.Ok(new
            {
                companiesRead = sourceCompanies.Count,
                contactsRead = sourceContacts.Count,
                created,
                updated,
                skipped,
                contactsCreated,
                contactsUpdated,
                relationsCreated,
                errors
            });
        });
    }

    private static async Task<int> EnsureRelationAsync(
        ApplicationDbContext db, Guid companyId, Guid contactId, bool isPrimary, CancellationToken ct)
    {
        var relations = db.Set<CompanyContact>();
        var relation = await relations.SingleOrDefaultAsync(x => x.CompanyId == companyId && x.ContactId == contactId, ct);

        if (relation is null)
        {
            relations.Add(new CompanyContact(companyId, contactId, isPrimary));
            await db.SaveChangesAsync(ct);
            return 1;
        }

        relation.Update(isPrimary || relation.IsPrimary, true);
        await db.SaveChangesAsync(ct);
        return 0;
    }

    private static CommercialPremiumOptions ReadOptions(IConfiguration configuration) => new(
        configuration["CommercialPremium:ConnectionString"] ?? string.Empty,
        configuration["CommercialPremium:AllowedDatabase"] ?? string.Empty);

    private static string? ValidateOptions(CommercialPremiumOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            return "Falta CommercialPremium__ConnectionString en el archivo .env.";
        if (string.IsNullOrWhiteSpace(options.AllowedDatabase))
            return "Falta CommercialPremium__AllowedDatabase en el archivo .env.";

        var builder = new SqlConnectionStringBuilder(options.ConnectionString);
        if (!string.Equals(builder.InitialCatalog, options.AllowedDatabase, StringComparison.OrdinalIgnoreCase))
            return $"La base configurada ({builder.InitialCatalog}) no coincide con AllowedDatabase ({options.AllowedDatabase}).";
        return null;
    }

    private static async Task<List<CommercialCompanyRow>> ReadCompaniesAsync(
        CommercialPremiumOptions options, int? limit, CancellationToken ct)
    {
        var rows = new List<CommercialCompanyRow>();
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 90;
        command.CommandText = $"""
            SELECT {(limit.HasValue ? $"TOP ({limit.Value})" : string.Empty)}
                CIDCLIENTEPROVEEDOR,
                LTRIM(RTRIM(ISNULL(CCODIGOCLIENTE, ''))),
                LTRIM(RTRIM(ISNULL(CRAZONSOCIAL, ''))),
                LTRIM(RTRIM(ISNULL(CRFC, ''))),
                LTRIM(RTRIM(ISNULL(CEMAIL1, ''))),
                LTRIM(RTRIM(ISNULL(CEMAIL2, ''))),
                LTRIM(RTRIM(ISNULL(CEMAIL3, '')))
            FROM admClientes
            WHERE CTIPOCLIENTE IN (1, 3) AND CESTATUS = 1
            ORDER BY CRAZONSOCIAL;
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new CommercialCompanyRow(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                EmptyToNull(reader.GetString(4)),
                EmptyToNull(reader.GetString(5)),
                EmptyToNull(reader.GetString(6))));
        return rows;
    }

    private static async Task<List<CommercialContactRow>> ReadContactsAsync(
        CommercialPremiumOptions options, int? limit, CancellationToken ct)
    {
        var rows = new List<CommercialContactRow>();
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 90;
        command.CommandText = $"""
            SELECT {(limit.HasValue ? $"TOP ({limit.Value})" : string.Empty)}
                CIDDIRECCION,
                CIDCATALOGO,
                LTRIM(RTRIM(ISNULL(CNOMBRECALLE, ''))),
                LTRIM(RTRIM(ISNULL(CEMAIL, ''))),
                LTRIM(RTRIM(ISNULL(CTELEFONO1, ''))),
                LTRIM(RTRIM(ISNULL(CTELEFONO2, '')))
            FROM admDomicilios
            WHERE CTIPOCATALOGO = 6 AND CIDCATALOGO > 0
            ORDER BY CIDCATALOGO, CIDDIRECCION;
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new CommercialContactRow(
                reader.GetInt32(0),
                reader.GetInt32(1),
                EmptyToNull(reader.GetString(2)),
                EmptyToNull(reader.GetString(3)),
                EmptyToNull(reader.GetString(4)),
                EmptyToNull(reader.GetString(5))));
        return rows;
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (parts.Length > 0 ? parts[0] : "Contacto", parts.Length > 1 ? parts[1] : string.Empty);
    }

    private static string? FirstEmail(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault()?.ToLowerInvariant();

    private static string NormalizeRfc(string value) => value.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CommercialPremiumOptions(string ConnectionString, string AllowedDatabase);
    private sealed record CommercialCompanyRow(
        int Id,
        string Code,
        string BusinessName,
        string Rfc,
        string? Email1,
        string? Email2,
        string? Email3);
    private sealed record CommercialContactRow(
        int AddressId,
        int CustomerId,
        string? Name,
        string? Email,
        string? Phone1,
        string? Phone2);
}
