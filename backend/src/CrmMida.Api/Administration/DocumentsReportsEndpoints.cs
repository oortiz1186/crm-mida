using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Administration;

public static class DocumentsReportsEndpoints
{
    public static void MapDocumentsReportsEndpoints(this WebApplication app)
    {
        var documents = app.MapGroup("/api/v1/companies/{companyId:guid}/documents");

        documents.MapGet("/", async (Guid companyId, ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = new List<object>();
            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "OriginalName", "ContentType", "SizeBytes", "Category", "Description", "CreatedAtUtc"
                FROM company_documents WHERE "CompanyId" = @companyId AND "IsActive" = TRUE
                ORDER BY "CreatedAtUtc" DESC;
                """;
            Add(command, "@companyId", companyId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new
                {
                    id = reader.GetGuid(0), originalName = reader.GetString(1), contentType = reader.GetString(2),
                    sizeBytes = reader.GetInt64(3), category = reader.GetString(4),
                    description = reader.IsDBNull(5) ? null : reader.GetString(5), createdAtUtc = reader.GetDateTime(6)
                });
            return Results.Ok(rows);
        }).RequireAuthorization("companies.read");

        documents.MapPost("/", async (
            Guid companyId, IFormFile file, string? category, string? description,
            ClaimsPrincipal user, ApplicationDbContext db, AuditService audit,
            IConfiguration configuration, CancellationToken ct) =>
        {
            if (!await db.Companies.AnyAsync(x => x.Id == companyId && x.IsActive, ct)) return Results.NotFound();
            if (file.Length == 0 || file.Length > 20 * 1024 * 1024) return Results.BadRequest(new { message = "El archivo debe pesar entre 1 byte y 20 MB." });
            var allowed = new[] { ".pdf", ".xlsx", ".xls", ".docx", ".doc", ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension)) return Results.BadRequest(new { message = "Tipo de archivo no permitido." });

            var id = Guid.NewGuid();
            var root = configuration["Documents:StoragePath"] ?? Path.Combine(AppContext.BaseDirectory, "storage", "documents");
            var companyFolder = Path.Combine(root, companyId.ToString("N"));
            Directory.CreateDirectory(companyFolder);
            var storedName = $"{id:N}{extension}";
            var fullPath = Path.Combine(companyFolder, storedName);
            await using (var output = File.Create(fullPath)) await file.CopyToAsync(output, ct);

            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO company_documents
                ("Id", "CompanyId", "OriginalName", "StoredName", "ContentType", "SizeBytes", "Category", "Description", "CreatedByUserId", "CreatedAtUtc", "IsActive")
                VALUES (@id, @companyId, @originalName, @storedName, @contentType, @sizeBytes, @category, @description, @userId, @created, TRUE);
                """;
            Add(command, "@id", id); Add(command, "@companyId", companyId); Add(command, "@originalName", Path.GetFileName(file.FileName));
            Add(command, "@storedName", storedName); Add(command, "@contentType", file.ContentType ?? "application/octet-stream");
            Add(command, "@sizeBytes", file.Length); Add(command, "@category", string.IsNullOrWhiteSpace(category) ? "general" : category.Trim());
            Add(command, "@description", string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim());
            var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;
            Add(command, "@userId", userId.HasValue ? userId.Value : DBNull.Value); Add(command, "@created", DateTime.UtcNow);
            await command.ExecuteNonQueryAsync(ct);
            await audit.WriteAsync(user, "upload", "CompanyDocument", id.ToString(), new { companyId, file = file.FileName, file.Length }, ct);
            return Results.Created($"/api/v1/companies/{companyId}/documents/{id}", new { id });
        }).DisableAntiforgery().RequireAuthorization("companies.manage");

        documents.MapGet("/{documentId:guid}/download", async (Guid companyId, Guid documentId, ApplicationDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            string? originalName = null; string? storedName = null; string? contentType = null;
            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT \"OriginalName\", \"StoredName\", \"ContentType\" FROM company_documents WHERE \"Id\"=@id AND \"CompanyId\"=@companyId AND \"IsActive\"=TRUE";
            Add(command, "@id", documentId); Add(command, "@companyId", companyId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct)) { originalName = reader.GetString(0); storedName = reader.GetString(1); contentType = reader.GetString(2); }
            if (storedName is null) return Results.NotFound();
            var root = configuration["Documents:StoragePath"] ?? Path.Combine(AppContext.BaseDirectory, "storage", "documents");
            var path = Path.Combine(root, companyId.ToString("N"), storedName);
            return File.Exists(path) ? Results.File(path, contentType ?? "application/octet-stream", originalName) : Results.NotFound();
        }).RequireAuthorization("companies.read");

        documents.MapDelete("/{documentId:guid}", async (Guid companyId, Guid documentId, ClaimsPrincipal user, ApplicationDbContext db, AuditService audit, CancellationToken ct) =>
        {
            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE company_documents SET \"IsActive\"=FALSE WHERE \"Id\"=@id AND \"CompanyId\"=@companyId AND \"IsActive\"=TRUE";
            Add(command, "@id", documentId); Add(command, "@companyId", companyId);
            var affected = await command.ExecuteNonQueryAsync(ct);
            if (affected == 0) return Results.NotFound();
            await audit.WriteAsync(user, "delete", "CompanyDocument", documentId.ToString(), new { companyId }, ct);
            return Results.NoContent();
        }).RequireAuthorization("companies.manage");

        var reports = app.MapGroup("/api/v1/reports").RequireAuthorization("companies.read");
        reports.MapGet("/pipeline.csv", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Opportunities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Stage).ThenBy(x => x.Name)
                .Select(x => new { x.Name, x.Stage, x.Status, x.EstimatedAmount, x.Probability, x.ExpectedCloseDateUtc }).ToListAsync(ct);
            return Csv("pipeline.csv", new[] { "Oportunidad", "Etapa", "Estado", "Monto", "Probabilidad", "Cierre estimado" },
                rows.Select(x => new[] { x.Name, x.Stage, x.Status, x.EstimatedAmount.ToString(CultureInfo.InvariantCulture), x.Probability.ToString(CultureInfo.InvariantCulture), x.ExpectedCloseDateUtc?.ToString("yyyy-MM-dd") ?? "" }));
        });
        reports.MapGet("/quotes.csv", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Quotes.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Folio, x.Title, x.Status, x.Total, x.Currency, x.ValidUntilUtc, x.CreatedAtUtc }).ToListAsync(ct);
            return Csv("cotizaciones.csv", new[] { "Folio", "Título", "Estado", "Total", "Moneda", "Vigencia", "Creada" },
                rows.Select(x => new[] { x.Folio, x.Title, x.Status, x.Total.ToString(CultureInfo.InvariantCulture), x.Currency, x.ValidUntilUtc.ToString("yyyy-MM-dd"), x.CreatedAtUtc.ToString("yyyy-MM-dd") }));
        });
        reports.MapGet("/activities.csv", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Activities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DueAtUtc)
                .Select(x => new { x.Subject, x.Type, x.Priority, x.Status, x.DueAtUtc, x.CompletedAtUtc }).ToListAsync(ct);
            return Csv("actividades.csv", new[] { "Asunto", "Tipo", "Prioridad", "Estado", "Vencimiento", "Completada" },
                rows.Select(x => new[] { x.Subject, x.Type, x.Priority, x.Status, x.DueAtUtc.ToString("s"), x.CompletedAtUtc?.ToString("s") ?? "" }));
        });
    }

    private static IResult Csv(string fileName, IEnumerable<string> headers, IEnumerable<string[]> rows)
    {
        static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', headers.Select(Escape)));
        foreach (var row in rows) builder.AppendLine(string.Join(',', row.Select(Escape)));
        return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(), "text/csv; charset=utf-8", fileName);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }
}
