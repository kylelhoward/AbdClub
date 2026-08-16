using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class SplitMemberNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new blank nullable columns to your PostgreSQL instance safely first
            migrationBuilder.AddColumn<string>(name: "FirstName", table: "Members", nullable: true);
            migrationBuilder.AddColumn<string>(name: "MiddleName", table: "Members", nullable: true);
            migrationBuilder.AddColumn<string>(name: "LastName", table: "Members", nullable: true);

            // 2. DATA PARSING STEP: Automatically splits existing names into your new columns natively inside SQL!
            migrationBuilder.Sql(@"
        UPDATE ""Members"" 
        SET 
            ""FirstName"" = split_part(""FullName"", ' ', 1),
            ""LastName"" = SUBSTRING(""FullName"" FROM POSITION(' ' IN ""FullName"") + 1)
        WHERE ""FullName"" IS NOT NULL;
    ");

            // 3. Enforce the required constraints now that data is populated
            migrationBuilder.AlterColumn<string>(name: "FirstName", table: "Members", nullable: false);
            migrationBuilder.AlterColumn<string>(name: "LastName", table: "Members", nullable: false);

            // 4. Safely delete the obsolete composite field column 
            migrationBuilder.DropColumn(name: "FullName", table: "Members");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Members");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Members",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
