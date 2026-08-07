using System;
using CrmMida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804170000_AddCompanyContactRelations")]
public partial class AddCompanyContactRelations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Email1", table: "contacts", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Email2", table: "contacts", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Email3", table: "contacts", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<int>(name: "ContpaqiCustomerId", table: "contacts", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ContpaqiAddressId", table: "contacts", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ContpaqiDatabase", table: "contacts", type: "character varying(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastSyncedAtUtc", table: "contacts", type: "timestamp with time zone", nullable: true);

        migrationBuilder.CreateTable(
            name: "company_contacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                Active = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_company_contacts", x => x.Id);
                table.ForeignKey(name: "FK_company_contacts_companies_CompanyId", column: x => x.CompanyId, principalTable: "companies", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_company_contacts_contacts_ContactId", column: x => x.ContactId, principalTable: "contacts", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_company_contacts_CompanyId_ContactId", table: "company_contacts", columns: new[] { "CompanyId", "ContactId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_company_contacts_ContactId", table: "company_contacts", column: "ContactId");
        migrationBuilder.CreateIndex(name: "IX_contacts_ContpaqiDatabase_ContpaqiAddressId", table: "contacts", columns: new[] { "ContpaqiDatabase", "ContpaqiAddressId" });

        migrationBuilder.Sql("""
            UPDATE contacts SET "Email1" = "Email" WHERE "Email1" IS NULL AND "Email" IS NOT NULL;
            INSERT INTO company_contacts ("Id", "CompanyId", "ContactId", "IsPrimary", "Active", "CreatedAtUtc", "UpdatedAtUtc", "IsActive")
            SELECT gen_random_uuid(), "CompanyId", "Id", "IsPrimary", TRUE, NOW(), NULL, TRUE
            FROM contacts
            ON CONFLICT DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "company_contacts");
        migrationBuilder.DropColumn(name: "Email1", table: "contacts");
        migrationBuilder.DropColumn(name: "Email2", table: "contacts");
        migrationBuilder.DropColumn(name: "Email3", table: "contacts");
        migrationBuilder.DropColumn(name: "ContpaqiCustomerId", table: "contacts");
        migrationBuilder.DropColumn(name: "ContpaqiAddressId", table: "contacts");
        migrationBuilder.DropColumn(name: "ContpaqiDatabase", table: "contacts");
        migrationBuilder.DropColumn(name: "LastSyncedAtUtc", table: "contacts");
    }
}
