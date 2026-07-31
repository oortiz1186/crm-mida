using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class InitialSecurityAndCommercial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS roles (
                "Id" uuid NOT NULL,
                "Name" character varying(100) NOT NULL,
                "Description" character varying(300),
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_roles" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_roles_Name" ON roles ("Name");

            CREATE TABLE IF NOT EXISTS permissions (
                "Id" uuid NOT NULL,
                "Code" character varying(150) NOT NULL,
                "Description" character varying(300),
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_permissions" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_permissions_Code" ON permissions ("Code");

            CREATE TABLE IF NOT EXISTS users (
                "Id" uuid NOT NULL,
                "FirstName" character varying(100) NOT NULL,
                "LastName" character varying(100) NOT NULL,
                "Email" character varying(200) NOT NULL,
                "PasswordHash" character varying(500) NOT NULL,
                "LastLoginAtUtc" timestamp with time zone,
                "IsLocked" boolean NOT NULL DEFAULT FALSE,
                "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
                "LockedUntilUtc" timestamp with time zone,
                "PasswordChangedAtUtc" timestamp with time zone,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_users" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Email" ON users ("Email");

            CREATE TABLE IF NOT EXISTS user_roles (
                "UserId" uuid NOT NULL,
                "RoleId" uuid NOT NULL,
                CONSTRAINT "PK_user_roles" PRIMARY KEY ("UserId", "RoleId"),
                CONSTRAINT "FK_user_roles_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_user_roles_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES roles ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_user_roles_RoleId" ON user_roles ("RoleId");

            CREATE TABLE IF NOT EXISTS role_permissions (
                "RoleId" uuid NOT NULL,
                "PermissionId" uuid NOT NULL,
                CONSTRAINT "PK_role_permissions" PRIMARY KEY ("RoleId", "PermissionId"),
                CONSTRAINT "FK_role_permissions_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES roles ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_role_permissions_permissions_PermissionId" FOREIGN KEY ("PermissionId") REFERENCES permissions ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_role_permissions_PermissionId" ON role_permissions ("PermissionId");

            CREATE TABLE IF NOT EXISTS companies (
                "Id" uuid NOT NULL,
                "TradeName" character varying(200) NOT NULL,
                "BusinessName" character varying(250) NOT NULL,
                "Rfc" character varying(13) NOT NULL,
                "TaxRegime" character varying(150),
                "FiscalPostalCode" character varying(10),
                "Email" character varying(200),
                "Phone" character varying(50),
                "Website" character varying(250),
                "Address" character varying(500),
                "City" character varying(120),
                "State" character varying(120),
                "CustomerType" character varying(80) NOT NULL,
                "Status" character varying(50) NOT NULL,
                "Tags" character varying(500),
                "ExternalContpaqiId" character varying(100),
                "AssignedUserId" uuid,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_companies" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_companies_users_AssignedUserId" FOREIGN KEY ("AssignedUserId") REFERENCES users ("Id") ON DELETE SET NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_companies_Rfc" ON companies ("Rfc");
            CREATE INDEX IF NOT EXISTS "IX_companies_AssignedUserId" ON companies ("AssignedUserId");
            CREATE INDEX IF NOT EXISTS "IX_companies_TradeName" ON companies ("TradeName");

            CREATE TABLE IF NOT EXISTS contacts (
                "Id" uuid NOT NULL,
                "CompanyId" uuid NOT NULL,
                "FirstName" character varying(100) NOT NULL,
                "LastName" character varying(100) NOT NULL,
                "Position" character varying(120),
                "Area" character varying(120),
                "Phone" character varying(50),
                "Mobile" character varying(50),
                "Email" character varying(200),
                "IsPrimary" boolean NOT NULL,
                "IsPurchasingContact" boolean NOT NULL,
                "IsTechnicalContact" boolean NOT NULL,
                "IsBillingContact" boolean NOT NULL,
                "MarketingConsent" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_contacts" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_contacts_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_contacts_CompanyId" ON contacts ("CompanyId");
            CREATE INDEX IF NOT EXISTS "IX_contacts_Email" ON contacts ("Email");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS contacts;
            DROP TABLE IF EXISTS companies;
            DROP TABLE IF EXISTS role_permissions;
            DROP TABLE IF EXISTS user_roles;
            DROP TABLE IF EXISTS permissions;
            DROP TABLE IF EXISTS roles;
            DROP TABLE IF EXISTS users;
            """);
    }
}
