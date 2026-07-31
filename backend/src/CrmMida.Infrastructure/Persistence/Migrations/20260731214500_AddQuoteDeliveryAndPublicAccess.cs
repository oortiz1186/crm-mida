using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddQuoteDeliveryAndPublicAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS quote_delivery_attempts (
                "Id" uuid NOT NULL,
                "QuoteId" uuid NOT NULL,
                "Channel" character varying(30) NOT NULL,
                "Recipient" character varying(250) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "ProviderReference" character varying(500),
                "ErrorMessage" character varying(2000),
                "AttemptNumber" integer NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "CompletedAtUtc" timestamp with time zone,
                CONSTRAINT "PK_quote_delivery_attempts" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_quote_delivery_attempts_quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES quotes ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_quote_delivery_attempts_QuoteId_CreatedAtUtc" ON quote_delivery_attempts ("QuoteId", "CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_quote_delivery_attempts_Status" ON quote_delivery_attempts ("Status");

            CREATE TABLE IF NOT EXISTS quote_public_accesses (
                "Id" uuid NOT NULL,
                "QuoteId" uuid NOT NULL,
                "TokenHash" character varying(64) NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "OpenedAtUtc" timestamp with time zone,
                "RespondedAtUtc" timestamp with time zone,
                "Decision" character varying(30),
                "DecisionComment" character varying(2000),
                "IsRevoked" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_quote_public_accesses" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_quote_public_accesses_quotes_QuoteId" FOREIGN KEY ("QuoteId") REFERENCES quotes ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_quote_public_accesses_TokenHash" ON quote_public_accesses ("TokenHash");
            CREATE INDEX IF NOT EXISTS "IX_quote_public_accesses_QuoteId" ON quote_public_accesses ("QuoteId");
            CREATE INDEX IF NOT EXISTS "IX_quote_public_accesses_ExpiresAtUtc" ON quote_public_accesses ("ExpiresAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS quote_public_accesses; DROP TABLE IF EXISTS quote_delivery_attempts;");
    }
}
