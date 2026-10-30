using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Ticket.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "transferred_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "tickets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "ticket_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_customer_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    from_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_customer_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_transfers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_transfers_ticket_id",
                table: "ticket_transfers",
                column: "ticket_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_transfers_to_email_status",
                table: "ticket_transfers",
                columns: new[] { "to_email", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_transfers");

            migrationBuilder.DropColumn(
                name: "transferred_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "tickets");
        }
    }
}
