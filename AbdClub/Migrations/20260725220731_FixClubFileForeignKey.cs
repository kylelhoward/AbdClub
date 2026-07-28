using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class FixClubFileForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubFiles_Members_UploadedById",
                table: "ClubFiles");

            migrationBuilder.DropIndex(
                name: "IX_ClubFiles_UploadedById",
                table: "ClubFiles");

            migrationBuilder.DropColumn(
                name: "UploadedById",
                table: "ClubFiles");

            migrationBuilder.CreateIndex(
                name: "IX_ClubFiles_UploadedByMemberId",
                table: "ClubFiles",
                column: "UploadedByMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubFiles_Members_UploadedById",
                table: "ClubFiles",
                column: "UploadedByMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubFiles_Members_UploadedById",
                table: "ClubFiles");

            migrationBuilder.DropIndex(
                name: "IX_ClubFiles_UploadedByMemberId",
                table: "ClubFiles");

            migrationBuilder.AddColumn<int>(
                name: "UploadedById",
                table: "ClubFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ClubFiles_UploadedById",
                table: "ClubFiles",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubFiles_Members_UploadedById",
                table: "ClubFiles",
                column: "UploadedById",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
