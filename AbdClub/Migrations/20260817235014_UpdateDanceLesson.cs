using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDanceLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanceAssignedInstructors");

            migrationBuilder.AddColumn<int>(
                name: "LessonId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_LessonId",
                table: "Events",
                column: "LessonId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Lessons_LessonId",
                table: "Events",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Lessons_LessonId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_LessonId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "Events");

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

            migrationBuilder.CreateIndex(
                name: "IX_DanceAssignedInstructors_DanceId",
                table: "DanceAssignedInstructors",
                column: "DanceId");
        }
    }
}
