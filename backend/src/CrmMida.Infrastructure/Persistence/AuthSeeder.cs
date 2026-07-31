using CrmMida.Application.Security;
using CrmMida.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CrmMida.Infrastructure.Persistence;

public sealed class AuthSeeder(ApplicationDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration)
{
    private static readonly string[] PermissionCodes =
    [
        "dashboard.read", "users.read", "users.manage", "roles.read", "roles.manage",
        "companies.read", "companies.manage", "contacts.read", "contacts.manage",
        "prospects.read", "prospects.manage", "opportunities.read", "opportunities.manage",
        "activities.read", "activities.manage", "quotes.read", "quotes.manage",
        "catalog.read", "catalog.manage"
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var administratorRole = await EnsureRoleAsync("Administrador", "Acceso completo al CRM MIDA.", cancellationToken);
        await EnsureRoleAsync("Gerente Comercial", "Administración del equipo comercial.", cancellationToken);
        await EnsureRoleAsync("Asesor Comercial", "Gestión de cartera y operaciones propias.", cancellationToken);
        await EnsureRoleAsync("Soporte", "Consulta de clientes y operación de soporte.", cancellationToken);

        foreach (var code in PermissionCodes)
            if (!await dbContext.Permissions.AnyAsync(x => x.Code == code, cancellationToken))
                dbContext.Permissions.Add(new Permission(code, $"Permiso {code}"));

        await dbContext.SaveChangesAsync(cancellationToken);
        var permissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var permission in permissions)
            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == administratorRole.Id && x.PermissionId == permission.Id, cancellationToken))
                dbContext.RolePermissions.Add(new RolePermission(administratorRole.Id, permission.Id));

        var email = configuration["CRM_ADMIN_EMAIL"]?.Trim().ToLowerInvariant();
        var password = configuration["CRM_ADMIN_PASSWORD"];
        var firstName = configuration["CRM_ADMIN_FIRST_NAME"]?.Trim() ?? "Administrador";
        var lastName = configuration["CRM_ADMIN_LAST_NAME"]?.Trim() ?? "MIDA";

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (user is null)
            {
                user = new User(firstName, lastName, email, passwordHasher.Hash(password));
                dbContext.Users.Add(user);
                dbContext.UserRoles.Add(new UserRole(user.Id, administratorRole.Id));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(string name, string description, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (role is not null) return role;
        role = new Role(name, description);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }
}
