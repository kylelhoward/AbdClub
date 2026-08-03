using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLessonsToInstructorIdRelationalModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Instructor",
                table: "Lessons");

            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_InstructorId",
                table: "Lessons",
                column: "InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_MasterInstructors_InstructorId",
                table: "Lessons",
                column: "InstructorId",
                principalTable: "MasterInstructors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_MasterInstructors_InstructorId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_InstructorId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Lessons");

            migrationBuilder.AddColumn<string>(
                name: "Instructor",
                table: "Lessons",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
