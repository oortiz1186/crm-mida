using System.Security.Claims;

namespace CrmMida.Api.Commercial;

public sealed class CommercialAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var isCompaniesPath = path.StartsWithSegments("/api/v1/companies");
        var isContactsPath = path.StartsWithSegments("/api/v1/contacts") ||
                             (isCompaniesPath && path.Value?.Contains("/contacts", StringComparison.OrdinalIgnoreCase) == true);

        if (!isCompaniesPath && !isContactsPath)
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var isRead = HttpMethods.IsGet(context.Request.Method);
        var requiredPermission = isContactsPath
            ? isRead ? "contacts.read" : "contacts.manage"
            : isRead ? "companies.read" : "companies.manage";

        var authorized = context.User.FindAll("permission")
            .Any(claim => string.Equals(claim.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

        if (!authorized)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "No cuentas con el permiso requerido.",
                permission = requiredPermission
            });
            return;
        }

        await next(context);
    }
}
