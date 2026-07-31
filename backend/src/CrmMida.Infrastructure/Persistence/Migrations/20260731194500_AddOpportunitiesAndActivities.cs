using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrmMida.Infrastructure.Persistence.Migrations;

public partial class AddOpportunitiesAndActivities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS opportunities (
                "Id" uuid NOT NULL,
                "Name" character varying(220) NOT NULL,
                "CompanyId" uuid NOT NULL,
                "ContactId" uuid,
                "ProspectId" uuid,
                "AssignedUserId" uuid,
                "ProductOrService" character varying(250),
                "EstimatedAmount" numeric(18,2) NOT NULL,
                "Probability" integer NOT NULL,
                "ExpectedCloseDateUtc" timestamp with time zone,
                "Stage" character varying(40) NOT NULL,
                "Status" character varying(40) NOT NULL,
                "LossReason" character varying(500),
                "Notes" character varying(2000),
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_opportunities" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_opportunities_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_opportunities_contacts_ContactId" FOREIGN KEY ("ContactId") REFERENCES contacts ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_opportunities_prospects_ProspectId" FOREIGN KEY ("ProspectId") REFERENCES prospects ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_opportunities_users_AssignedUserId" FOREIGN KEY ("AssignedUserId") REFERENCES users ("Id") ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_opportunities_Stage" ON opportunities ("Stage");
            CREATE INDEX IF NOT EXISTS "IX_opportunities_ExpectedCloseDateUtc" ON opportunities ("ExpectedCloseDateUtc");
            CREATE INDEX IF NOT EXISTS "IX_opportunities_CompanyId" ON opportunities ("CompanyId");

            CREATE TABLE IF NOT EXISTS activities (
                "Id" uuid NOT NULL,
                "Type" character varying(40) NOT NULL,
                "Subject" character varying(220) NOT NULL,
                "Description" character varying(2000),
                "DueAtUtc" timestamp with time zone NOT NULL,
                "Priority" character varying(30) NOT NULL,
                "Status" character varying(30) NOT NULL,
                "AssignedUserId" uuid,
                "OpportunityId" uuid,
                "ProspectId" uuid,
                "CompanyId" uuid,
                "CompletedAtUtc" timestamp with time zone,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone,
                "IsActive" boolean NOT NULL,
                CONSTRAINT "PK_activities" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_activities_opportunities_OpportunityId" FOREIGN KEY ("OpportunityId") REFERENCES opportunities ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_activities_prospects_ProspectId" FOREIGN KEY ("ProspectId") REFERENCES prospects ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_activities_companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES companies ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_activities_users_AssignedUserId" FOREIGN KEY ("AssignedUserId") REFERENCES users ("Id") ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_activities_DueAtUtc" ON activities ("DueAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_activities_Status" ON activities ("Status");
            CREATE INDEX IF NOT EXISTS "IX_activities_OpportunityId" ON activities ("OpportunityId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS activities;
            DROP TABLE IF EXISTS opportunities;
            """);
    }
}
