using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731220500_AddCompanyDocuments")]
public partial class AddCompanyDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS company_documents (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CompanyId" uuid NOT NULL,
                "OriginalName" varchar(260) NOT NULL,
                "StoredName" varchar(260) NOT NULL,
                "ContentType" varchar(150) NOT NULL,
                "SizeBytes" bigint NOT NULL,
                "Category" varchar(80) NOT NULL,
                "Description" varchar(1000) NULL,
                "CreatedByUserId" uuid NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "IsActive" boolean NOT NULL DEFAULT TRUE,
                CONSTRAINT "FK_company_documents_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_company_documents_users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES users ("Id") ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_company_documents_CompanyId" ON company_documents ("CompanyId");
            CREATE INDEX IF NOT EXISTS "IX_company_documents_CreatedAtUtc" ON company_documents ("CreatedAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS company_documents;");
    }
}
