using AbdClub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbdClub.Migrations
{
    [DbContext(typeof(AbdContext))]
    [Migration("20260828193000_AddPaymentMethodAndNotes")]
    public partial class AddPaymentMethodAndNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Payments\" SET \"PaymentMethod\" = " +
                "CASE WHEN \"TransactionId\" LIKE 'manual\\_%' ESCAPE '\\' " +
                "THEN 'Manual (unspecified)' " +
                "WHEN \"TransactionId\" LIKE 'pi\\_%' ESCAPE '\\' " +
                "OR \"TransactionId\" LIKE 'cs\\_%' ESCAPE '\\' THEN 'Stripe' " +
                "ELSE 'Unspecified' END " +
                "WHERE \"PaymentMethod\" IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Notes", table: "Payments");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "Payments");
        }
    }
}
