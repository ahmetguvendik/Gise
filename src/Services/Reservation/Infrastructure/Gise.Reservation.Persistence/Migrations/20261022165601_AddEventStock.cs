using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Reservation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "key",
                table: "inbox_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "inbox_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "number",
                table: "event_seats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "event_seats",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "row",
                table: "event_seats",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                table: "event_seats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "section_name",
                table: "event_seats",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "event_stocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_stocks", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "fk_event_seats_event_stocks_event_id",
                table: "event_seats",
                column: "event_id",
                principalTable: "event_stocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_event_seats_event_stocks_event_id",
                table: "event_seats");

            migrationBuilder.DropTable(
                name: "event_stocks");

            migrationBuilder.DropColumn(
                name: "key",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "type",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "number",
                table: "event_seats");

            migrationBuilder.DropColumn(
                name: "price",
                table: "event_seats");

            migrationBuilder.DropColumn(
                name: "row",
                table: "event_seats");

            migrationBuilder.DropColumn(
                name: "section_id",
                table: "event_seats");

            migrationBuilder.DropColumn(
                name: "section_name",
                table: "event_seats");
        }
    }
}
