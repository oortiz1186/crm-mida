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
                readOnly = true
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
            command.CommandText = "SELECT DB_NAME(), COUNT(*) FROM admClientes WHERE CTIPOCLIENTE IN (1, 3);";
            await using var reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            return Results.Ok(new
            {
                connected = true,
                database = reader.GetString(0),
                companies = reader.GetInt32(1),
                readOnly = true
            });
        });

        group.MapGet("/preview", async (int? limit, IConfiguration configuration, CancellationToken ct) =>
        {
            var options = ReadOptions(configuration);
            var validation = ValidateOptions(options);
            if (validation is not null) return Results.BadRequest(new { message = validation });

            var take = Math.Clamp(limit ?? 20, 1, 100);
            var rows = await ReadCompaniesAsync(options, take, ct);
            return Results.Ok(new { totalPreview = rows.Count, items = rows });
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

            var source = await ReadCompaniesAsync(options, null, ct);
            var created = 0;
            var updated = 0;
            var skipped = 0;
            var contactsCreated = 0;
            var errors = new List<object>();

            foreach (var item in source)
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
                    var company = await db.Companies
                        .Include(x => x.Contacts)
                        .SingleOrDefaultAsync(x => x.ExternalContpaqiId == externalId, ct);

                    company ??= await db.Companies
                        .Include(x => x.Contacts)
                        .SingleOrDefaultAsync(x => x.Rfc == rfc, ct);

                    var tradeName = string.IsNullOrWhiteSpace(item.TradeName) ? item.BusinessName : item.TradeName;
                    if (company is null)
                    {
                        company = new Company(tradeName, item.BusinessName, rfc, "client");
                        db.Companies.Add(company);
                        created++;
                    }
                    else
                    {
                        updated++;
                    }

                    company.Update(
                        tradeName,
                        item.BusinessName,
                        rfc,
                        null,
                        item.PostalCode,
                        item.Email,
                        item.Phone,
                        null,
                        item.Address,
                        item.City,
                        item.State,
                        "client",
                        "active",
                        "CONTPAQi",
                        externalId,
                        company.AssignedUserId);

                    if (!string.IsNullOrWhiteSpace(item.ContactName))
                    {
                        var parts = item.ContactName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        var firstName = parts[0];
                        var lastName = parts.Length > 1 ? parts[1] : string.Empty;
                        var normalizedEmail = item.Email?.Trim().ToLowerInvariant();
                        var exists = company.Contacts.Any(x =>
                            (!string.IsNullOrWhiteSpace(normalizedEmail) && x.Email == normalizedEmail) ||
                            (x.FirstName == firstName && x.LastName == lastName));
                        if (!exists)
                        {
                            var contact = new Contact(company.Id, firstName, lastName, normalizedEmail);
                            contact.Update(firstName, lastName, "Contacto CONTPAQi", null, item.Phone, null,
                                normalizedEmail, company.Contacts.Count == 0, false, false, false, false);
                            company.Contacts.Add(contact);
                            contactsCreated++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new { item.Id, item.BusinessName, error = ex.Message });
                }
            }

            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, "sync", "CommercialPremium", null,
                new { source = source.Count, created, updated, skipped, contactsCreated, errors = errors.Count }, ct);

            return Results.Ok(new { source = source.Count, created, updated, skipped, contactsCreated, errors });
        });
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
        CommercialPremiumOptions options,
        int? limit,
        CancellationToken ct)
    {
        var rows = new List<CommercialCompanyRow>();
        await using var connection = new SqlConnection(options.ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = $"""
            SELECT {(limit.HasValue ? $"TOP ({limit.Value})" : string.Empty)}
                CIDCLIENTEPROVEEDOR,
                LTRIM(RTRIM(ISNULL(CCODIGOCLIENTE, ''))),
                LTRIM(RTRIM(ISNULL(CRAZONSOCIAL, ''))),
                LTRIM(RTRIM(ISNULL(CRFC, ''))),
                LTRIM(RTRIM(ISNULL(CEMAIL1, ''))),
                LTRIM(RTRIM(ISNULL(CTELEFONO1, ''))),
                LTRIM(RTRIM(ISNULL(CDOMICILIO, ''))),
                LTRIM(RTRIM(ISNULL(CCIUDAD, ''))),
                LTRIM(RTRIM(ISNULL(CESTADO, ''))),
                LTRIM(RTRIM(ISNULL(CCODIGOPOSTAL, ''))),
                LTRIM(RTRIM(ISNULL(CCONTACTO, '')))
            FROM admClientes
            WHERE CTIPOCLIENTE IN (1, 3) AND CESTATUS = 1
            ORDER BY CRAZONSOCIAL;
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new CommercialCompanyRow(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                EmptyToNull(reader.GetString(4)), EmptyToNull(reader.GetString(5)), EmptyToNull(reader.GetString(6)),
                EmptyToNull(reader.GetString(7)), EmptyToNull(reader.GetString(8)), EmptyToNull(reader.GetString(9)),
                EmptyToNull(reader.GetString(10))));
        }
        return rows;
    }

    private static string NormalizeRfc(string value) => value.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CommercialPremiumOptions(string ConnectionString, string AllowedDatabase);
    private sealed record CommercialCompanyRow(int Id, string TradeName, string BusinessName, string Rfc,
        string? Email, string? Phone, string? Address, string? City, string? State, string? PostalCode, string? ContactName);
}
