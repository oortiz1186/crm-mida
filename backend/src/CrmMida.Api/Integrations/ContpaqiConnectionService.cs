using Microsoft.Data.SqlClient;

namespace CrmMida.Api.Integrations;

public sealed class ContpaqiConnectionService(IConfiguration configuration)
{
    public ContpaqiIntegrationStatus GetStatus()
    {
        var connectionString = configuration["Contpaqi:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return new(false, false, null, null, null, "La conexión CONTPAQi no está configurada.");

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return new(true, false, builder.DataSource, builder.InitialCatalog, null, "Configuración disponible; falta probar la conexión.");
        }
        catch (ArgumentException)
        {
            return new(true, false, null, null, null, "La cadena de conexión configurada no es válida.");
        }
    }

    public async Task<ContpaqiConnectionTestResult> TestAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration["Contpaqi:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return ContpaqiConnectionTestResult.Failure("La conexión CONTPAQi no está configurada.");

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = Math.Clamp(
                    int.TryParse(configuration["Contpaqi:ConnectTimeoutSeconds"], out var timeout) ? timeout : 8,
                    3,
                    30)
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var serverVersion = connection.ServerVersion;
            var tables = await DetectTablesAsync(connection, cancellationToken);
            var isCommercialPremium = tables.Contains("admClientes") && tables.Contains("admProductos");

            return new(
                true,
                builder.DataSource,
                connection.Database,
                serverVersion,
                isCommercialPremium,
                tables,
                isCommercialPremium
                    ? "Conexión correcta y estructura compatible con CONTPAQi Comercial Premium."
                    : "Conexión correcta, pero no se detectaron todas las tablas esperadas.");
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException or ArgumentException)
        {
            return ContpaqiConnectionTestResult.Failure(Sanitize(exception.Message));
        }
    }

    private static async Task<IReadOnlyCollection<string>> DetectTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var expected = new[] { "admClientes", "admProductos", "admDocumentos", "admMovimientos", "admMovimientosSerie", "admConceptos" };
        var found = new List<string>();

        const string sql = "SELECT OBJECT_ID(@tableName, 'U');";
        foreach (var table in expected)
        {
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tableName", table);
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is not null && value is not DBNull) found.Add(table);
        }

        return found;
    }

    private static string Sanitize(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "No fue posible conectar con CONTPAQi.";
        return message.Length <= 500 ? message : message[..500];
    }
}

public sealed record ContpaqiIntegrationStatus(
    bool Configured,
    bool Connected,
    string? Server,
    string? Database,
    string? ServerVersion,
    string Message);

public sealed record ContpaqiConnectionTestResult(
    bool Success,
    string? Server,
    string? Database,
    string? ServerVersion,
    bool CommercialPremiumDetected,
    IReadOnlyCollection<string> DetectedTables,
    string Message)
{
    public static ContpaqiConnectionTestResult Failure(string message) =>
        new(false, null, null, null, false, Array.Empty<string>(), message);
}
