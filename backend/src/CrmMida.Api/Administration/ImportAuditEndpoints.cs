using System.Data;
using System.Security.Claims;
using ClosedXML.Excel;
using CrmMida.Domain.Commercial;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Administration;

public static class ImportAuditEndpoints
{
    public static void MapImportAuditEndpoints(this WebApplication app)
    {
        var import = app.MapGroup("/api/v1/import").RequireAuthorization("companies.manage");

        import.MapPost("/companies", async (
            IFormFile file,
            ClaimsPrincipal user,
            ApplicationDbContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            if (file.Length == 0) return Results.BadRequest(new { message = "El archivo está vacío." });
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Solo se permiten archivos .xlsx." });

            var jobId = Guid.NewGuid();
            var started = DateTime.UtcNow;
            var created = 0;
            var skipped = 0;
            var errors = new List<object>();

            try
            {
                await using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var sheet = workbook.Worksheets.First();
                var headerRow = sheet.FirstRowUsed();
                if (headerRow is null) return Results.BadRequest(new { message = "El archivo no contiene datos." });

                var headers = headerRow.CellsUsed()
                    .ToDictionary(c => NormalizeHeader(c.GetString()), c => c.Address.ColumnNumber);

                foreach (var required in new[] { "nombrecomercial", "razonsocial", "rfc" })
                    if (!headers.ContainsKey(required))
                        return Results.BadRequest(new { message = $"Falta la columna obligatoria: {required}." });

                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    var rowNumber = row.RowNumber();
                    var tradeName = Cell(row, headers, "nombrecomercial");
                    var businessName = Cell(row, headers, "razonsocial");
                    var rfc = Cell(row, headers, "rfc").Replace(" ", string.Empty).ToUpperInvariant();

                    if (string.IsNullOrWhiteSpace(tradeName) || string.IsNullOrWhiteSpace(businessName) || rfc.Length is < 12 or > 13)
                    {
                        errors.Add(new { row = rowNumber, message = "Nombre comercial, razón social o RFC inválido." });
                        continue;
                    }

                    if (await db.Companies.AnyAsync(x => x.Rfc == rfc, ct))
                    {
                        skipped++;
                        continue;
                    }

                    var company = new Company(tradeName, businessName, rfc, ValueOr(Cell(row, headers, "tipocliente"), "client"));
                    company.Update(
                        tradeName,
                        businessName,
                        rfc,
                        Cell(row, headers, "regimenfiscal"),
                        Cell(row, headers, "codigopostal"),
                        Cell(row, headers, "correo"),
                        Cell(row, headers, "telefono"),
                        Cell(row, headers, "sitioweb"),
                        Cell(row, headers, "direccion"),
                        Cell(row, headers, "ciudad"),
                        Cell(row, headers, "estado"),
                        ValueOr(Cell(row, headers, "tipocliente"), "client"),
                        "active",
                        Cell(row, headers, "etiquetas"),
                        null,
                        null);
                    db.Companies.Add(company);
                    created++;
                }

                await db.SaveChangesAsync(ct);
                await InsertImportJobAsync(db, jobId, file.FileName, "completed", created, skipped, errors.Count, started, DateTime.UtcNow, ct);
                await audit.WriteAsync(user, "import", "Company", null, new { jobId, file = file.FileName, created, skipped, errors = errors.Count }, ct);
                return Results.Ok(new { jobId, created, skipped, errors });
            }
            catch (Exception ex)
            {
                await InsertImportJobAsync(db, jobId, file.FileName, "failed", created, skipped, errors.Count + 1, started, DateTime.UtcNow, ct, ex.Message);
                await audit.WriteAsync(user, "import_failed", "Company", null, new { jobId, file = file.FileName, error = ex.Message }, ct);
                return Results.Problem(title: "No fue posible importar el archivo.", detail: ex.Message, statusCode: 500);
            }
        }).DisableAntiforgery();

        import.MapGet("/jobs", async (ApplicationDbContext db, CancellationToken ct) =>
        {
            var rows = new List<object>();
            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "FileName", "Status", "CreatedRecords", "SkippedRecords", "ErrorRecords",
                       "StartedAtUtc", "CompletedAtUtc", "ErrorMessage"
                FROM import_jobs ORDER BY "StartedAtUtc" DESC LIMIT 100;
                """;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new
                {
                    id = reader.GetGuid(0), fileName = reader.GetString(1), status = reader.GetString(2),
                    createdRecords = reader.GetInt32(3), skippedRecords = reader.GetInt32(4), errorRecords = reader.GetInt32(5),
                    startedAtUtc = reader.GetDateTime(6), completedAtUtc = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                    errorMessage = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            return Results.Ok(rows);
        });

        app.MapGet("/api/v1/audit", async (
            string? action,
            string? entityType,
            int? limit,
            ApplicationDbContext db,
            CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 100, 1, 500);
            var rows = new List<object>();
            await using var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "UserId", "UserEmail", "Action", "EntityType", "EntityId", "DetailsJson", "CreatedAtUtc"
                FROM audit_logs
                WHERE (@action IS NULL OR "Action" = @action)
                  AND (@entityType IS NULL OR "EntityType" = @entityType)
                ORDER BY "CreatedAtUtc" DESC LIMIT @take;
                """;
            Add(command, "@action", string.IsNullOrWhiteSpace(action) ? DBNull.Value : action);
            Add(command, "@entityType", string.IsNullOrWhiteSpace(entityType) ? DBNull.Value : entityType);
            Add(command, "@take", take);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new
                {
                    id = reader.GetGuid(0), userId = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                    userEmail = reader.IsDBNull(2) ? null : reader.GetString(2), action = reader.GetString(3),
                    entityType = reader.GetString(4), entityId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    detailsJson = reader.IsDBNull(6) ? null : reader.GetString(6), createdAtUtc = reader.GetDateTime(7)
                });
            return Results.Ok(rows);
        }).RequireAuthorization("companies.manage");
    }

    private static string NormalizeHeader(string value) => new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    private static string Cell(IXLRow row, IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var column) ? row.Cell(column).GetFormattedString().Trim() : string.Empty;
    private static string ValueOr(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static async Task InsertImportJobAsync(ApplicationDbContext db, Guid id, string fileName, string status,
        int created, int skipped, int errors, DateTime started, DateTime? completed, CancellationToken ct, string? error = null)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO import_jobs
            ("Id", "FileName", "Status", "CreatedRecords", "SkippedRecords", "ErrorRecords", "StartedAtUtc", "CompletedAtUtc", "ErrorMessage")
            VALUES (@id, @file, @status, @created, @skipped, @errors, @started, @completed, @error);
            """;
        Add(command, "@id", id); Add(command, "@file", fileName); Add(command, "@status", status);
        Add(command, "@created", created); Add(command, "@skipped", skipped); Add(command, "@errors", errors);
        Add(command, "@started", started); Add(command, "@completed", completed.HasValue ? completed.Value : DBNull.Value);
        Add(command, "@error", string.IsNullOrWhiteSpace(error) ? DBNull.Value : error);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }
}
