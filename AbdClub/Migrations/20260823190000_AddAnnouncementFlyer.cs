using System;
using AbdClub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    [DbContext(typeof(AbdContext))]
    [Migration("20260823190000_AddAnnouncementFlyer")]
    public partial class AddAnnouncementFlyer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnouncementFlyerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Greeting = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MembershipUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnouncementFlyerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlyerAnnouncementItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnouncementFlyerSettingsId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlyerAnnouncementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlyerAnnouncementItems_AnnouncementFlyerSettings_Announcem~",
                        column: x => x.AnnouncementFlyerSettingsId,
                        principalTable: "AnnouncementFlyerSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelpWantedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnouncementFlyerSettingsId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpWantedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpWantedItems_AnnouncementFlyerSettings_AnnouncementFly~",
                        column: x => x.AnnouncementFlyerSettingsId,
                        principalTable: "AnnouncementFlyerSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AnnouncementFlyerSettings",
                columns: new[] { "Id", "Greeting", "MembershipUrl", "UpdatedAt", "WebsiteUrl" },
                values: new object[]
                {
                    1,
                    "Thank you for coming tonight!",
                    "https://www.danceatx.org/store/annual-membership-1",
                    new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc),
                    "https://www.danceatx.org/"
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlyerAnnouncementItems_AnnouncementFlyerSettingsId",
                table: "FlyerAnnouncementItems",
                column: "AnnouncementFlyerSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpWantedItems_AnnouncementFlyerSettingsId",
                table: "HelpWantedItems",
                column: "AnnouncementFlyerSettingsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FlyerAnnouncementItems");
            migrationBuilder.DropTable(name: "HelpWantedItems");
            migrationBuilder.DropTable(name: "AnnouncementFlyerSettings");
        }
    }
}
