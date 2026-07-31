using CrmMida.Domain.Commercial;
using CrmMida.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace CrmMida.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FailedLoginAttempts).HasDefaultValue(0);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TradeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.BusinessName).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Rfc).HasMaxLength(13).IsRequired();
            entity.Property(x => x.TaxRegime).HasMaxLength(150);
            entity.Property(x => x.FiscalPostalCode).HasMaxLength(10);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Website).HasMaxLength(250);
            entity.Property(x => x.Address).HasMaxLength(350);
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.State).HasMaxLength(120);
            entity.Property(x => x.CustomerType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Tags).HasMaxLength(500);
            entity.Property(x => x.ExternalContpaqiId).HasMaxLength(100);
            entity.HasIndex(x => x.Rfc).IsUnique();
            entity.HasIndex(x => x.TradeName);
            entity.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Position).HasMaxLength(120);
            entity.Property(x => x.Area).HasMaxLength(120);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Mobile).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.HasIndex(x => new { x.CompanyId, x.Email });
            entity.HasOne(x => x.Company).WithMany(x => x.Contacts).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Prospect>(entity =>
        {
            entity.ToTable("prospects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(250);
            entity.Property(x => x.Rfc).HasMaxLength(13);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Source).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Interest).HasMaxLength(250);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Qualification).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Email);
            entity.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ConvertedCompany).WithMany().HasForeignKey(x => x.ConvertedCompanyId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("opportunities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.ProductOrService).HasMaxLength(250);
            entity.Property(x => x.EstimatedAmount).HasPrecision(18, 2);
            entity.Property(x => x.Stage).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.LossReason).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.Stage);
            entity.HasIndex(x => x.ExpectedCloseDateUtc);
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Prospect).WithMany().HasForeignKey(x => x.ProspectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Priority).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.DueAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Opportunity).WithMany(x => x.Activities).HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Prospect).WithMany().HasForeignKey(x => x.ProspectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
