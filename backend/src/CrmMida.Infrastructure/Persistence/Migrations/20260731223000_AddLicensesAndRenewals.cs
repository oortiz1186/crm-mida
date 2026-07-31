using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddLicensesAndRenewals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS licenses (
                "Id" uuid PRIMARY KEY,
                "CompanyId" uuid NOT NULL REFERENCES companies("Id") ON DELETE RESTRICT,
                "ProductName" varchar(250) NOT NULL,
                "SerialNumber" varchar(120) NOT NULL,
                "Version" varchar(80) NULL,
                "LicenseType" varchar(80) NULL,
                "Users" integer NOT NULL DEFAULT 1,
                "Companies" integer NOT NULL DEFAULT 1,
                "StartsAtUtc" timestamp with time zone NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL,
                "Status" varchar(30) NOT NULL,
                "Notes" varchar(2000) NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_licenses_SerialNumber ON licenses("SerialNumber");
            CREATE INDEX IF NOT EXISTS IX_licenses_CompanyId ON licenses("CompanyId");
            CREATE INDEX IF NOT EXISTS IX_licenses_ExpiresAtUtc ON licenses("ExpiresAtUtc");
            CREATE INDEX IF NOT EXISTS IX_licenses_Status ON licenses("Status");

            CREATE TABLE IF NOT EXISTS renewal_opportunities (
                "Id" uuid PRIMARY KEY,
                "LicenseId" uuid NOT NULL REFERENCES licenses("Id") ON DELETE CASCADE,
                "TargetDateUtc" timestamp with time zone NOT NULL,
                "EstimatedAmount" numeric(18,2) NOT NULL DEFAULT 0,
                "Status" varchar(30) NOT NULL,
                "OpportunityId" uuid NULL REFERENCES opportunities("Id") ON DELETE SET NULL,
                "Notes" varchar(2000) NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "CompletedAtUtc" timestamp with time zone NULL
            );
            CREATE INDEX IF NOT EXISTS IX_renewal_opportunities_LicenseId_Status ON renewal_opportunities("LicenseId","Status");
            CREATE INDEX IF NOT EXISTS IX_renewal_opportunities_TargetDateUtc ON renewal_opportunities("TargetDateUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS renewal_opportunities; DROP TABLE IF EXISTS licenses;");
    }
}
