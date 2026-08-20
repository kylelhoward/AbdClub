using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEmailLogAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_Members_MemberId",
                table: "EmailLogs");

            migrationBuilder.RenameColumn(
                name: "Opened",
                table: "EmailLogs",
                newName: "IsSuccess");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "EmailLogs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "MemberId",
                table: "EmailLogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "EmailType",
                table: "EmailLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "EmailLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "EmailLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientEmail",
                table: "EmailLogs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "EmailLogs",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_Members_MemberId",
                table: "EmailLogs",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_Members_MemberId",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "RecipientEmail",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "EmailLogs");

            migrationBuilder.RenameColumn(
                name: "IsSuccess",
                table: "EmailLogs",
                newName: "Opened");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "EmailLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "MemberId",
                table: "EmailLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmailType",
                table: "EmailLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_Members_MemberId",
                table: "EmailLogs",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
