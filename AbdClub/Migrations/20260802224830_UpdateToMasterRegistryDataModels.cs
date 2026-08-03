using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToMasterRegistryDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanceAttendingOfficers");

            migrationBuilder.DropTable(
                name: "DanceHost");

            migrationBuilder.DropTable(
                name: "Djs");

            migrationBuilder.DropTable(
                name: "Volunteers");

            migrationBuilder.AddColumn<int>(
                name: "DanceId",
                table: "Members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedDjId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MasterDjs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterDjs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterHosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterInstructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterInstructors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterVolunteers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterVolunteers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DanceAssignedHosts",
                columns: table => new
                {
                    AssignedHostsId = table.Column<int>(type: "integer", nullable: false),
                    DanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceAssignedHosts", x => new { x.AssignedHostsId, x.DanceId });
                    table.ForeignKey(
                        name: "FK_DanceAssignedHosts_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanceAssignedHosts_MasterHosts_AssignedHostsId",
                        column: x => x.AssignedHostsId,
                        principalTable: "MasterHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanceAssignedInstructors",
                columns: table => new
                {
                    AssignedInstructorsId = table.Column<int>(type: "integer", nullable: false),
                    DanceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceAssignedInstructors", x => new { x.AssignedInstructorsId, x.DanceId });
                    table.ForeignKey(
                        name: "FK_DanceAssignedInstructors_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanceAssignedInstructors_MasterInstructors_AssignedInstruct~",
                        column: x => x.AssignedInstructorsId,
                        principalTable: "MasterInstructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Members_DanceId",
                table: "Members",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_AssignedDjId",
                table: "Events",
                column: "AssignedDjId");

            migrationBuilder.CreateIndex(
                name: "IX_DanceAssignedHosts_DanceId",
                table: "DanceAssignedHosts",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DanceAssignedInstructors_DanceId",
                table: "DanceAssignedInstructors",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DanceAssignedVolunteers_DanceId",
                table: "DanceAssignedVolunteers",
                column: "DanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_MasterDjs_AssignedDjId",
                table: "Events",
                column: "AssignedDjId",
                principalTable: "MasterDjs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Events_DanceId",
                table: "Members",
                column: "DanceId",
                principalTable: "Events",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_MasterDjs_AssignedDjId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Events_DanceId",
                table: "Members");

            migrationBuilder.DropTable(
                name: "DanceAssignedHosts");

            migrationBuilder.DropTable(
                name: "DanceAssignedInstructors");

            migrationBuilder.DropTable(
                name: "DanceAssignedVolunteers");

            migrationBuilder.DropTable(
                name: "MasterDjs");

            migrationBuilder.DropTable(
                name: "MasterHosts");

            migrationBuilder.DropTable(
                name: "MasterInstructors");

            migrationBuilder.DropTable(
                name: "MasterVolunteers");

            migrationBuilder.DropIndex(
                name: "IX_Members_DanceId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Events_AssignedDjId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DanceId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "AssignedDjId",
                table: "Events");

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

            migrationBuilder.CreateTable(
                name: "DanceHost",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DanceId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Djs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DanceId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Djs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Djs_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Volunteers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DanceId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volunteers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Volunteers_Events_DanceId",
                        column: x => x.DanceId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanceAttendingOfficers_DanceId",
                table: "DanceAttendingOfficers",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_DanceHost_DanceId",
                table: "DanceHost",
                column: "DanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Djs_DanceId",
                table: "Djs",
                column: "DanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_DanceId",
                table: "Volunteers",
                column: "DanceId");
        }
    }
}
