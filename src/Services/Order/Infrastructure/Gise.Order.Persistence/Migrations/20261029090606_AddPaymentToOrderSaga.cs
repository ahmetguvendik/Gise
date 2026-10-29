using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Order.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentToOrderSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "last_payment_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "order_sagas",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_id",
                table: "order_sagas",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_payment_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "order_sagas");

            migrationBuilder.DropColumn(
                name: "payment_id",
                table: "order_sagas");
        }
    }
}
