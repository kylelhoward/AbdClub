using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class IsolateOfficerAttendanceToJunctionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Events_DanceId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_DanceId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "DanceId",
                table: "Members");

            migrationBuilder.CreateTable(
                name: "DanceAttendingOfficers",
                columns: table => new
                {
                    AttendingOfficersId = table.Column<int>(type: "integer", nullable: false),
                    DanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceAttendingOfficers", x => new { x.AttendingOfficersId, x.DanceId });
                    table.ForeignKey(
                        name: "FK_DanceAttendingOfficers_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanceAttendingOfficers_Members_AttendingOfficersId",
                        column: x => x.AttendingOfficersId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanceAttendingOfficers_DanceId",
                table: "DanceAttendingOfficers",
                column: "DanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanceAttendingOfficers");

            migrationBuilder.AddColumn<int>(
                name: "DanceId",
                table: "Members",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_DanceId",
                table: "Members",
                column: "DanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Events_DanceId",
                table: "Members",
                column: "DanceId",
                principalTable: "Events",
                principalColumn: "Id");
        }
    }
}
