using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731214000_AddImportJobsAndAuditLogs")]
public partial class AddImportJobsAndAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                UserEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DetailsJson = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_audit_logs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "import_jobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CreatedRecords = table.Column<int>(type: "integer", nullable: false),
                SkippedRecords = table.Column<int>(type: "integer", nullable: false),
                ErrorRecords = table.Column<int>(type: "integer", nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ErrorMessage = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_import_jobs", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_audit_logs_CreatedAtUtc", table: "audit_logs", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_audit_logs_EntityType_EntityId", table: "audit_logs", columns: new[] { "EntityType", "EntityId" });
        migrationBuilder.CreateIndex(name: "IX_import_jobs_StartedAtUtc", table: "import_jobs", column: "StartedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_logs");
        migrationBuilder.DropTable(name: "import_jobs");
    }
}
