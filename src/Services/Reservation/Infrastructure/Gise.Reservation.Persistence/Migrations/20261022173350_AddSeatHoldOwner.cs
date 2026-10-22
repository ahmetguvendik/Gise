using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Reservation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatHoldOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_seat_holds_expires_at",
                table: "seat_holds");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "seat_holds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "seat_ids",
                table: "seat_holds",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "seat_holds",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_event_id_user_id",
                table: "seat_holds",
                columns: new[] { "event_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_seat_holds_event_id_user_id",
                table: "seat_holds");

            migrationBuilder.DropColumn(
                name: "seat_ids",
                table: "seat_holds");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "seat_holds");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "seat_holds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_expires_at",
                table: "seat_holds",
                column: "expires_at",
                filter: "status = 'Active'");
        }
    }
}
