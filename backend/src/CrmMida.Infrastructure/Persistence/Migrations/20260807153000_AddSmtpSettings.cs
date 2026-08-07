using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260807153000_AddSmtpSettings")]
public partial class AddSmtpSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS smtp_settings (
                "Id" integer NOT NULL PRIMARY KEY,
                "Host" varchar(250) NOT NULL DEFAULT '',
                "Port" integer NOT NULL DEFAULT 587,
                "EnableSsl" boolean NOT NULL DEFAULT TRUE,
                "UserName" varchar(250) NULL,
                "Password" varchar(1000) NULL,
                "FromEmail" varchar(250) NOT NULL DEFAULT '',
                "FromName" varchar(250) NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS smtp_settings;");
    }
}
