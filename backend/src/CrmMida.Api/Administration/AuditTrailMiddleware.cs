using System.Security.Claims;

namespace CrmMida.Api.Administration;

public sealed class AuditTrailMiddleware(RequestDelegate next, ILogger<AuditTrailMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, AuditService audit)
    {
        await next(context);

        if (!context.User.Identity?.IsAuthenticated ?? true) return;
        if (context.Request.Method is not ("POST" or "PUT" or "PATCH" or "DELETE")) return;
        if (!context.Request.Path.StartsWithSegments("/api/v1")) return;
        if (context.Request.Path.StartsWithSegments("/api/v1/auth")) return;
        if (context.Response.StatusCode >= 400) return;

        try
        {
            var action = context.Request.Method switch
            {
                "POST" => "create_or_execute",
                "PUT" => "update",
                "PATCH" => "update_status",
                "DELETE" => "delete",
                _ => "write"
            };

            await audit.WriteAsync(
                context.User,
                action,
                ResolveEntityType(context.Request.Path),
                ResolveEntityId(context.Request.RouteValues),
                new
                {
                    method = context.Request.Method,
                    path = context.Request.Path.Value,
                    statusCode = context.Response.StatusCode,
                    traceId = context.TraceIdentifier
                },
                context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No fue posible registrar la auditoría de {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }

    private static string ResolveEntityType(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return segments.Length >= 3 ? segments[2] : "ApiOperation";
    }

    private static string? ResolveEntityId(RouteValueDictionary values)
    {
        foreach (var key in new[] { "id", "companyId", "quoteId", "documentId", "accessId" })
            if (values.TryGetValue(key, out var value) && value is not null) return value.ToString();
        return null;
    }
}
