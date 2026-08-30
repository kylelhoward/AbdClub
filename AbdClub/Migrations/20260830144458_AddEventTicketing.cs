using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTicketing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventTicketOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    PurchaserName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PurchaserEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PurchaserPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ManualTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmationEmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketOrders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventTicketTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SalesStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsMemberOnly = table.Column<bool>(type: "boolean", nullable: false),
                    IsDoorPrice = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    TicketTypeId = table.Column<int>(type: "integer", nullable: false),
                    HolderName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: true),
                    TicketCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TicketTypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PricePaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInByOfficerAccountId = table.Column<int>(type: "integer", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeRefundId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTickets_EventTicketOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "EventTicketOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventTickets_EventTicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "EventTicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventTickets_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventTickets_OfficerAccounts_CheckedInByOfficerAccountId",
                        column: x => x.CheckedInByOfficerAccountId,
                        principalTable: "OfficerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_EventId",
                table: "EventTicketOrders",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_StripeCheckoutSessionId",
                table: "EventTicketOrders",
                column: "StripeCheckoutSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketOrders_StripePaymentIntentId",
                table: "EventTicketOrders",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_CheckedInByOfficerAccountId",
                table: "EventTickets",
                column: "CheckedInByOfficerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_MemberId",
                table: "EventTickets",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_OrderId",
                table: "EventTickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_TicketCode",
                table: "EventTickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTickets_TicketTypeId",
                table: "EventTickets",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTicketTypes_EventId_Name",
                table: "EventTicketTypes",
                columns: new[] { "EventId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventTickets");

            migrationBuilder.DropTable(
                name: "EventTicketOrders");

            migrationBuilder.DropTable(
                name: "EventTicketTypes");
        }
    }
}
