using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTypesPhaseOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanceAssignedVolunteers");

            migrationBuilder.DropTable(
                name: "DanceAttendingOfficers");

            migrationBuilder.AddColumn<int>(
                name: "EntertainmentType",
                table: "MasterDjs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "Events",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);

            migrationBuilder.AddColumn<string>(
                name: "ExternalWebsiteUrl",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationInstructions",
                table: "Events",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventAssignedVolunteers",
                columns: table => new
                {
                    AssignedVolunteersId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAssignedVolunteers", x => new { x.AssignedVolunteersId, x.EventId });
                    table.ForeignKey(
                        name: "FK_EventAssignedVolunteers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAssignedVolunteers_MasterVolunteers_AssignedVolunteers~",
                        column: x => x.AssignedVolunteersId,
                        principalTable: "MasterVolunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendingOfficers",
                columns: table => new
                {
                    AttendingOfficersId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendingOfficers", x => new { x.AttendingOfficersId, x.EventId });
                    table.ForeignKey(
                        name: "FK_EventAttendingOfficers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAttendingOfficers_Members_AttendingOfficersId",
                        column: x => x.AttendingOfficersId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventAssignedVolunteers_EventId",
                table: "EventAssignedVolunteers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendingOfficers_EventId",
                table: "EventAttendingOfficers",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventAssignedVolunteers");

            migrationBuilder.DropTable(
                name: "EventAttendingOfficers");

            migrationBuilder.DropColumn(
                name: "EntertainmentType",
                table: "MasterDjs");

            migrationBuilder.DropColumn(
                name: "ExternalWebsiteUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationInstructions",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "Events",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.CreateTable(
                name: "DanceAssignedVolunteers",
                columns: table => new
                {
                    AssignedVolunteersId = table.Column<int>(type: "integer", nullable: false),
                    DanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceAssignedVolunteers", x => new { x.AssignedVolunteersId, x.DanceId });
                    table.ForeignKey(
                        name: "FK_DanceAssignedVolunteers_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanceAssignedVolunteers_MasterVolunteers_AssignedVolunteers~",
                        column: x => x.AssignedVolunteersId,
                        principalTable: "MasterVolunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_DanceAssignedVolunteers_DanceId",
                table: "DanceAssignedVolunteers",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DanceAttendingOfficers_DanceId",
                table: "DanceAttendingOfficers",
                column: "DanceId");
        }
    }
}
