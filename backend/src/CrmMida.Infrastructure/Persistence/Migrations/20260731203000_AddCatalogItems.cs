using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddCatalogItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS catalog_items (
                "Id" uuid NOT NULL,
                "Code" character varying(50) NOT NULL,
                "Name" character varying(250) NOT NULL,
                "Type" character varying(30) NOT NULL,
                "Description" character varying(1000),
                "UnitPrice" numeric(18,2) NOT NULL,
                "TaxRate" numeric(8,4) NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                CONSTRAINT "PK_catalog_items" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_catalog_items_Code" ON catalog_items ("Code");
            CREATE INDEX IF NOT EXISTS "IX_catalog_items_Type_IsActive" ON catalog_items ("Type", "IsActive");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS catalog_items;");
    }
}
