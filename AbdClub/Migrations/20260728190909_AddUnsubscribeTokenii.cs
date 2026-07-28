using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class AddUnsubscribeTokenii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnsubscribeToken",
                table: "NewsletterSubscribers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnsubscribeToken",
                table: "NewsletterSubscribers");
        }
    }
}
