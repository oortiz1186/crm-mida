using System.Security.Claims;
using System.Text;
using CrmMida.Api.Auth;
using CrmMida.Api.Commercial;
using CrmMida.Api.Integrations;
using CrmMida.Application;
using CrmMida.Application.Security;
using CrmMida.Infrastructure;
using CrmMida.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<QuotePdfService>();
builder.Services.AddScoped<QuoteDeliveryService>();
builder.Services.AddScoped<LicenseAlertProcessor>();
builder.Services.AddScoped<ContpaqiConnectionService>();
builder.Services.AddHostedService<LicenseAlertBackgroundService>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("La configuración JWT no está disponible.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)), ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in new[]
    {
        "companies.read", "companies.manage", "contacts.read", "contacts.manage",
        "prospects.read", "prospects.manage", "opportunities.read", "opportunities.manage",
        "activities.read", "activities.manage", "quotes.read", "quotes.manage",
        "catalog.read", "catalog.manage", "licenses.read", "licenses.manage",
        "integration.manage"
    }) options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
});

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CommercialAuthorizationMiddleware>();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AuthSeeder>().SeedAsync();
}

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok", service = "CRM MIDA API", utc = DateTime.UtcNow }));

app.MapPost("/api/v1/auth/login", async (LoginRequest request, ApplicationDbContext dbContext, IPasswordHasher passwordHasher, JwtTokenService tokenService, CancellationToken cancellationToken) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await dbContext.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x.RolePermissions).ThenInclude(x => x.Permission).SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
    if (user is null || !user.CanLogin(DateTime.UtcNow)) return Results.Unauthorized();
    if (!passwordHasher.Verify(request.Password, user.PasswordHash)) { user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15)); await dbContext.SaveChangesAsync(cancellationToken); return Results.Unauthorized(); }
    user.RegisterSuccessfulLogin(); await dbContext.SaveChangesAsync(cancellationToken);
    var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().Order().ToArray();
    var permissions = user.UserRoles.SelectMany(x => x.Role.RolePermissions).Select(x => x.Permission.Code).Distinct().Order().ToArray();
    return Results.Ok(tokenService.Create(new CurrentUserDto(user.Id, user.Email, $"{user.FirstName} {user.LastName}".Trim(), roles, permissions)));
});

app.MapGet("/api/v1/auth/me", (ClaimsPrincipal principal) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Results.Unauthorized();
    return Results.Ok(new CurrentUserDto(id, principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty, principal.Identity?.Name ?? string.Empty, principal.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray(), principal.FindAll("permission").Select(x => x.Value).Distinct().ToArray()));
}).RequireAuthorization();

app.MapCommercialEndpoints();
app.MapProspectEndpoints();
app.MapOpportunityEndpoints();
app.MapQuoteEndpoints();
app.MapCatalogEndpoints();
app.MapQuotePortalEndpoints();
app.MapQuoteAccessManagementEndpoints();
app.MapLicenseEndpoints();
app.MapLicenseDashboardEndpoints();
app.MapLicenseAlertEndpoints();
app.MapContpaqiEndpoints();

app.Run();
public partial class Program;
