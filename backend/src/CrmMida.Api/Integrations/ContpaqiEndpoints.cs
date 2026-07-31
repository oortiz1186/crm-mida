namespace CrmMida.Api.Integrations;

public static class ContpaqiEndpoints
{
    public static void MapContpaqiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/integrations/contpaqi")
            .RequireAuthorization("integration.manage");

        group.MapGet("/status", (ContpaqiConnectionService service) =>
            Results.Ok(service.GetStatus()));

        group.MapPost("/test", async (ContpaqiConnectionService service, CancellationToken cancellationToken) =>
        {
            var result = await service.TestAsync(cancellationToken);
            return result.Success ? Results.Ok(result) : Results.Conflict(result);
        });
    }
}
