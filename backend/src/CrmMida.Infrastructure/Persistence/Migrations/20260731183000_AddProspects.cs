using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddProspects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS prospects (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "CompanyName" character varying(250),
                "Rfc" character varying(13),
                "Email" character varying(200),
                "Phone" character varying(30),
                "Source" character varying(100) NOT NULL,
                "Interest" character varying(250),
                "Status" character varying(40) NOT NULL,
                "Qualification" character varying(40) NOT NULL,
                "Notes" character varying(2000),
                "AssignedUserId" uuid,
                "ConvertedCompanyId" uuid,
                "ConvertedAtUtc" timestamp with time zone,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_prospects" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_prospects_users_AssignedUserId" FOREIGN KEY ("AssignedUserId") REFERENCES users ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_prospects_companies_ConvertedCompanyId" FOREIGN KEY ("ConvertedCompanyId") REFERENCES companies ("Id") ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_prospects_Name" ON prospects ("Name");
            CREATE INDEX IF NOT EXISTS "IX_prospects_Status" ON prospects ("Status");
            CREATE INDEX IF NOT EXISTS "IX_prospects_Email" ON prospects ("Email");
            CREATE INDEX IF NOT EXISTS "IX_prospects_AssignedUserId" ON prospects ("AssignedUserId");
            CREATE INDEX IF NOT EXISTS "IX_prospects_ConvertedCompanyId" ON prospects ("ConvertedCompanyId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS prospects;");
    }
}
