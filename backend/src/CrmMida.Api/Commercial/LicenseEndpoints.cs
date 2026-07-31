using System.Data;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Commercial;

public static class LicenseEndpoints
{
    public static void MapLicenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/licenses").RequireAuthorization();

        group.MapGet("/", async (string? search, string? status, int? expiringInDays, ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = await ReadLicensesAsync(db, search, status, expiringInDays, null, ct);
            return Results.Ok(rows);
        }).RequireAuthorization("licenses.read");

        group.MapGet("/{id:guid}", async (Guid id, ApplicationDbContext db, CancellationToken ct) =>
        {
            var row = (await ReadLicensesAsync(db, null, null, null, id, ct)).SingleOrDefault();
            return row is null ? Results.NotFound() : Results.Ok(row);
        }).RequireAuthorization("licenses.read");

        group.MapPost("/", async (SaveLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await db.Companies.AnyAsync(x => x.Id == request.CompanyId && x.IsActive, ct))
                return Results.BadRequest(new { message = "La empresa no existe." });
            if (request.ExpiresAtUtc <= request.StartsAtUtc)
                return Results.BadRequest(new { message = "La vigencia final debe ser posterior al inicio." });

            var serial = NormalizeSerial(request.SerialNumber);
            if (await SerialExistsAsync(db, serial, null, ct))
                return Results.Conflict(new { message = "El número de serie ya está registrado." });

            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;
            await ExecuteAsync(db, """
                INSERT INTO licenses
                ("Id","CompanyId","ProductName","SerialNumber","Version","LicenseType","Users","Companies","StartsAtUtc","ExpiresAtUtc","Status","Notes","CreatedAtUtc")
                VALUES (@id,@companyId,@product,@serial,@version,@type,@users,@companies,@starts,@expires,@status,@notes,@created);
                """, ct,
                ("@id", id), ("@companyId", request.CompanyId), ("@product", request.ProductName.Trim()),
                ("@serial", serial), ("@version", DbValue(request.Version)), ("@type", DbValue(request.LicenseType)),
                ("@users", Math.Max(1, request.Users)), ("@companies", Math.Max(1, request.Companies)),
                ("@starts", request.StartsAtUtc), ("@expires", request.ExpiresAtUtc),
                ("@status", StatusFor(request.ExpiresAtUtc, now)), ("@notes", DbValue(request.Notes)), ("@created", now));

            var created = (await ReadLicensesAsync(db, null, null, null, id, ct)).Single();
            return Results.Created($"/api/v1/licenses/{id}", created);
        }).RequireAuthorization("licenses.manage");

        group.MapPut("/{id:guid}", async (Guid id, SaveLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (request.ExpiresAtUtc <= request.StartsAtUtc)
                return Results.BadRequest(new { message = "La vigencia final debe ser posterior al inicio." });
            if (!(await ReadLicensesAsync(db, null, null, null, id, ct)).Any()) return Results.NotFound();

            await ExecuteAsync(db, """
                UPDATE licenses SET
                "ProductName"=@product,"Version"=@version,"LicenseType"=@type,"Users"=@users,"Companies"=@companies,
                "StartsAtUtc"=@starts,"ExpiresAtUtc"=@expires,"Status"=@status,"Notes"=@notes,"UpdatedAtUtc"=@updated
                WHERE "Id"=@id;
                """, ct,
                ("@product", request.ProductName.Trim()), ("@version", DbValue(request.Version)),
                ("@type", DbValue(request.LicenseType)), ("@users", Math.Max(1, request.Users)),
                ("@companies", Math.Max(1, request.Companies)), ("@starts", request.StartsAtUtc),
                ("@expires", request.ExpiresAtUtc), ("@status", StatusFor(request.ExpiresAtUtc, DateTime.UtcNow)),
                ("@notes", DbValue(request.Notes)), ("@updated", DateTime.UtcNow), ("@id", id));

            return Results.Ok((await ReadLicensesAsync(db, null, null, null, id, ct)).Single());
        }).RequireAuthorization("licenses.manage");

        group.MapPost("/{id:guid}/renewals", async (Guid id, CreateRenewalRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = (await ReadLicensesAsync(db, null, null, null, id, ct)).SingleOrDefault();
            if (license is null) return Results.NotFound();
            if (await HasPendingRenewalAsync(db, id, ct))
                return Results.Conflict(new { message = "Ya existe una renovación pendiente." });

            var target = request.TargetDateUtc ?? license.ExpiresAtUtc;
            var opportunity = new Opportunity($"Renovación {license.ProductName} · {license.SerialNumber}", license.CompanyId, request.EstimatedAmount);
            opportunity.Update(opportunity.Name, license.CompanyId, null, null, null, "Renovación de licencia", request.EstimatedAmount, 50, target, "prospecting", "open", null, $"Serie {license.SerialNumber}");
            db.Opportunities.Add(opportunity);

            var renewalId = Guid.NewGuid();
            await ExecuteAsync(db, """
                INSERT INTO renewal_opportunities
                ("Id","LicenseId","TargetDateUtc","EstimatedAmount","Status","OpportunityId","CreatedAtUtc")
                VALUES (@id,@licenseId,@target,@amount,'pending',@opportunityId,@created);
                """, ct, ("@id", renewalId), ("@licenseId", id), ("@target", target),
                ("@amount", Math.Max(0, request.EstimatedAmount)), ("@opportunityId", opportunity.Id), ("@created", DateTime.UtcNow));
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/licenses/{id}/renewals/{renewalId}", new { id = renewalId, status = "pending", targetDateUtc = target, request.EstimatedAmount, opportunityId = opportunity.Id });
        }).RequireAuthorization("licenses.manage");

        group.MapPost("/{id:guid}/renew", async (Guid id, RenewLicenseRequest request, ApplicationDbContext db, CancellationToken ct) =>
        {
            var license = (await ReadLicensesAsync(db, null, null, null, id, ct)).SingleOrDefault();
            if (license is null) return Results.NotFound();
            if (request.NewExpiresAtUtc <= license.ExpiresAtUtc)
                return Results.BadRequest(new { message = "La nueva vigencia debe ampliar la licencia." });

            await ExecuteAsync(db, """
                UPDATE licenses SET "ExpiresAtUtc"=@expires,"Status"=@status,"UpdatedAtUtc"=@updated WHERE "Id"=@id;
                UPDATE renewal_opportunities SET "Status"='renewed',"Notes"=@notes,"CompletedAtUtc"=@updated
                WHERE "LicenseId"=@id AND "Status"='pending';
                """, ct, ("@expires", request.NewExpiresAtUtc), ("@status", StatusFor(request.NewExpiresAtUtc, DateTime.UtcNow)),
                ("@updated", DateTime.UtcNow), ("@notes", DbValue(request.Notes)), ("@id", id));

            return Results.Ok((await ReadLicensesAsync(db, null, null, null, id, ct)).Single());
        }).RequireAuthorization("licenses.manage");
    }

    private static async Task<List<LicenseDto>> ReadLicensesAsync(ApplicationDbContext db, string? search, string? status, int? expiringInDays, Guid? id, CancellationToken ct)
    {
        var rows = new List<LicenseDto>();
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l."Id",l."CompanyId",c."TradeName",l."ProductName",l."SerialNumber",l."Version",l."LicenseType",
                   l."Users",l."Companies",l."StartsAtUtc",l."ExpiresAtUtc",l."Status",l."Notes"
            FROM licenses l INNER JOIN companies c ON c."Id"=l."CompanyId"
            WHERE (@id IS NULL OR l."Id"=@id)
              AND (@search IS NULL OR LOWER(l."ProductName") LIKE @term OR LOWER(l."SerialNumber") LIKE @term OR LOWER(c."TradeName") LIKE @term)
              AND (@status IS NULL OR l."Status"=@status)
              AND (@limitDate IS NULL OR (l."ExpiresAtUtc">=@now AND l."ExpiresAtUtc"<=@limitDate))
            ORDER BY l."ExpiresAtUtc";
            """;
        Add(command, "@id", id.HasValue ? id.Value : DBNull.Value);
        Add(command, "@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim());
        Add(command, "@term", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search.Trim().ToLowerInvariant()}%");
        Add(command, "@status", string.IsNullOrWhiteSpace(status) ? DBNull.Value : status.Trim().ToLowerInvariant());
        Add(command, "@now", DateTime.UtcNow);
        Add(command, "@limitDate", expiringInDays.HasValue ? DateTime.UtcNow.AddDays(Math.Clamp(expiringInDays.Value, 1, 365)) : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var expires = reader.GetDateTime(10);
            rows.Add(new LicenseDto(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt32(7), reader.GetInt32(8),
                reader.GetDateTime(9), expires, StatusFor(expires, DateTime.UtcNow), (int)Math.Ceiling((expires.Date - DateTime.UtcNow.Date).TotalDays),
                reader.IsDBNull(12) ? null : reader.GetString(12)));
        }
        return rows;
    }

    private static async Task<bool> SerialExistsAsync(ApplicationDbContext db, string serial, Guid? excludedId, CancellationToken ct) =>
        Convert.ToInt32(await ScalarAsync(db, "SELECT COUNT(*) FROM licenses WHERE \"SerialNumber\"=@serial AND (@id IS NULL OR \"Id\"<>@id);", ct,
            ("@serial", serial), ("@id", excludedId.HasValue ? excludedId.Value : DBNull.Value))) > 0;

    private static async Task<bool> HasPendingRenewalAsync(ApplicationDbContext db, Guid licenseId, CancellationToken ct) =>
        Convert.ToInt32(await ScalarAsync(db, "SELECT COUNT(*) FROM renewal_opportunities WHERE \"LicenseId\"=@id AND \"Status\"='pending';", ct, ("@id", licenseId))) > 0;

    private static async Task<object?> ScalarAsync(ApplicationDbContext db, string sql, CancellationToken ct, params (string Name, object Value)[] values)
    {
        await using var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand(); command.CommandText = sql;
        foreach (var value in values) Add(command, value.Name, value.Value);
        return await command.ExecuteScalarAsync(ct);
    }

    private static async Task ExecuteAsync(ApplicationDbContext db, string sql, CancellationToken ct, params (string Name, object Value)[] values)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand(); command.CommandText = sql;
        foreach (var value in values) Add(command, value.Name, value.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    private static string NormalizeSerial(string value) => value.Trim().ToUpperInvariant().Replace(" ", string.Empty);
    private static string StatusFor(DateTime expiresAtUtc, DateTime now) => expiresAtUtc < now ? "expired" : (expiresAtUtc.Date - now.Date).TotalDays <= 30 ? "expiring" : "active";
}

public sealed record SaveLicenseRequest(Guid CompanyId, string ProductName, string SerialNumber, string? Version, string? LicenseType, int Users, int Companies, DateTime StartsAtUtc, DateTime ExpiresAtUtc, string? Notes);
public sealed record CreateRenewalRequest(DateTime? TargetDateUtc, decimal EstimatedAmount = 0);
public sealed record RenewLicenseRequest(DateTime NewExpiresAtUtc, string? Notes);
public sealed record LicenseDto(Guid Id, Guid CompanyId, string CompanyName, string ProductName, string SerialNumber, string? Version, string? LicenseType, int Users, int Companies, DateTime StartsAtUtc, DateTime ExpiresAtUtc, string Status, int DaysToExpire, string? Notes);
