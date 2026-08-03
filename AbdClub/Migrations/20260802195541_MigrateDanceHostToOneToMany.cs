using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class MigrateDanceHostToOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Volunteers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Volunteers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Djs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Djs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DanceHost",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceHost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanceHost_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanceHost_DanceId",
                table: "DanceHost",
                column: "DanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanceHost");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Djs");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Djs");
        }
    }
}
