using System.Security.Claims;
using CrmMida.Application.Security;
using CrmMida.Domain.Security;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Api.Administration;

public static class UserAdministrationEndpoints
{
    public static void MapUserAdministrationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/administration").RequireAuthorization("companies.manage");

        group.MapGet("/roles", async (ApplicationDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Roles.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    permissions = x.RolePermissions
                        .Select(rp => rp.Permission.Code)
                        .OrderBy(code => code)
                        .ToList()
                }).ToListAsync(ct)));

        group.MapGet("/users", async (ApplicationDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Users.AsNoTracking()
                .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Email,
                    x.IsActive,
                    x.LastLoginAtUtc,
                    x.FailedLoginAttempts,
                    x.LockedUntilUtc,
                    roles = x.UserRoles
                        .Select(ur => new { ur.RoleId, ur.Role.Name })
                        .OrderBy(r => r.Name)
                        .ToList()
                }).ToListAsync(ct)));

        group.MapPost("/users", async (
            CreateUserRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            IPasswordHasher hasher,
            AuditService audit,
            CancellationToken ct) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(email) || request.Password.Length < 8)
                return Results.BadRequest(new { message = "Nombre, apellidos, correo y contraseña de al menos 8 caracteres son obligatorios." });

            if (await db.Users.AnyAsync(x => x.Email == email, ct))
                return Results.Conflict(new { message = "Ya existe un usuario con ese correo." });

            var roles = await db.Roles.Where(x => request.RoleIds.Contains(x.Id)).ToListAsync(ct);
            if (roles.Count != request.RoleIds.Distinct().Count())
                return Results.BadRequest(new { message = "Uno o más roles no existen." });

            var user = new User(request.FirstName, request.LastName, email, hasher.Hash(request.Password));
            db.Users.Add(user);
            foreach (var role in roles) db.UserRoles.Add(new UserRole(user.Id, role.Id));
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, "create", "User", user.Id.ToString(), new { user.Email, roles = roles.Select(x => x.Name) }, ct);
            return Results.Created($"/api/v1/administration/users/{user.Id}", new { user.Id });
        });

        group.MapPut("/users/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            var user = await db.Users.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(x => x.Email == email && x.Id != id, ct))
                return Results.Conflict(new { message = "Ya existe otro usuario con ese correo." });

            var roles = await db.Roles.Where(x => request.RoleIds.Contains(x.Id)).ToListAsync(ct);
            if (roles.Count != request.RoleIds.Distinct().Count())
                return Results.BadRequest(new { message = "Uno o más roles no existen." });

            user.UpdateProfile(request.FirstName, request.LastName, email);
            db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var role in roles) db.UserRoles.Add(new UserRole(user.Id, role.Id));
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, "update", "User", id.ToString(), new { user.Email, roles = roles.Select(x => x.Name) }, ct);
            return Results.NoContent();
        });

        group.MapPost("/users/{id:guid}/status", async (
            Guid id,
            UserStatusRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            var currentUserId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
            if (!request.Active && currentUserId == id)
                return Results.BadRequest(new { message = "No puedes desactivar tu propia cuenta." });
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();
            if (request.Active) user.Activate(); else user.Deactivate();
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, request.Active ? "activate" : "deactivate", "User", id.ToString(), null, ct);
            return Results.NoContent();
        });

        group.MapPost("/users/{id:guid}/unlock", async (
            Guid id,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();
            user.Unlock();
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, "unlock", "User", id.ToString(), null, ct);
            return Results.NoContent();
        });

        group.MapPost("/users/{id:guid}/password", async (
            Guid id,
            ResetPasswordRequest request,
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            IPasswordHasher hasher,
            AuditService audit,
            CancellationToken ct) =>
        {
            if (request.Password.Length < 8)
                return Results.BadRequest(new { message = "La contraseña debe tener al menos 8 caracteres." });
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (user is null) return Results.NotFound();
            user.ChangePassword(hasher.Hash(request.Password));
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync(principal, "reset_password", "User", id.ToString(), null, ct);
            return Results.NoContent();
        });
    }
}

public sealed record CreateUserRequest(string FirstName, string LastName, string Email, string Password, Guid[] RoleIds);
public sealed record UpdateUserRequest(string FirstName, string LastName, string Email, Guid[] RoleIds);
public sealed record UserStatusRequest(bool Active);
public sealed record ResetPasswordRequest(string Password);
