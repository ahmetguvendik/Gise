using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "events");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "events");
        }
    }
}
