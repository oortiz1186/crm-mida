using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddQuotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "quotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Folio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                ValidUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_quotes", x => x.Id);
                table.ForeignKey("FK_quotes_companies_CompanyId", x => x.CompanyId, "companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_quotes_contacts_ContactId", x => x.ContactId, "contacts", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_quotes_opportunities_OpportunityId", x => x.OpportunityId, "opportunities", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "quote_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                TaxRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Tax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_quote_items", x => x.Id);
                table.ForeignKey("FK_quote_items_quotes_QuoteId", x => x.QuoteId, "quotes", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_quotes_Folio", "quotes", "Folio", unique: true);
        migrationBuilder.CreateIndex("IX_quotes_Status", "quotes", "Status");
        migrationBuilder.CreateIndex("IX_quotes_CompanyId", "quotes", "CompanyId");
        migrationBuilder.CreateIndex("IX_quotes_ContactId", "quotes", "ContactId");
        migrationBuilder.CreateIndex("IX_quotes_OpportunityId", "quotes", "OpportunityId");
        migrationBuilder.CreateIndex("IX_quote_items_QuoteId", "quote_items", "QuoteId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "quote_items");
        migrationBuilder.DropTable(name: "quotes");
    }
}
