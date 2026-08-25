using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class SeparateOfficerAccountsAndMemberNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "MemberNumberSequence",
                startValue: 10001L);

            migrationBuilder.AddColumn<int>(
                name: "MemberNumber",
                table: "Members",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('\"MemberNumberSequence\"')");

            migrationBuilder.CreateTable(
                name: "OfficerAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    GoogleSubId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    OfficerTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficerAccounts_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });
            migrationBuilder.Sql("""
    INSERT INTO "OfficerAccounts"
        ("Email", "GoogleSubId", "AccessLevel", "OfficerTitle", "IsEnabled", "MemberId", "CreatedAt")
    SELECT DISTINCT ON (lower("Email"))
        lower("Email"),
        "GoogleSubId",
        CASE
            WHEN "IsTechAdmin" THEN 3
            WHEN "IsAdmin" THEN 2
            ELSE 1
        END,
        "OfficerRole",
        TRUE,
        "Id",
        NOW()
    FROM "Members"
    WHERE "IsOfficer" OR "IsAdmin" OR "IsTechAdmin"
    ORDER BY lower("Email"), "IsTechAdmin" DESC, "IsAdmin" DESC, "IsOfficer" DESC, "Id";
    """);

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberNumber",
                table: "Members",
                column: "MemberNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAccounts_Email",
                table: "OfficerAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAccounts_GoogleSubId",
                table: "OfficerAccounts",
                column: "GoogleSubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficerAccounts_MemberId",
                table: "OfficerAccounts",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfficerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Members_MemberNumber",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MemberNumber",
                table: "Members");

            migrationBuilder.DropSequence(
                name: "MemberNumberSequence");
        }
    }
}
