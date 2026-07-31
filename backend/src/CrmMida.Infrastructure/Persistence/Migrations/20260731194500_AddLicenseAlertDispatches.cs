using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddLicenseAlertDispatches : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "license_alert_dispatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                AlertType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                AlertDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ActivityId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_license_alert_dispatches", x => x.Id);
                table.ForeignKey("FK_license_alert_dispatches_licenses_LicenseId", x => x.LicenseId, "licenses", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_license_alert_dispatches_activities_ActivityId", x => x.ActivityId, "activities", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_license_alert_dispatches_LicenseId_AlertType_AlertDateUtc",
            table: "license_alert_dispatches",
            columns: new[] { "LicenseId", "AlertType", "AlertDateUtc" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "license_alert_dispatches");
}
