using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Reservation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TidySeatHoldsAndPrefixMassTransit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_message_inbox_state_inbox_message_id_inbox_consumer_",
                table: "outbox_message");

            migrationBuilder.DropForeignKey(
                name: "fk_outbox_message_outbox_state_outbox_id",
                table: "outbox_message");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "pk_outbox_state",
                table: "outbox_state");

            migrationBuilder.DropPrimaryKey(
                name: "pk_outbox_message",
                table: "outbox_message");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inbox_state",
                table: "inbox_state");

            migrationBuilder.RenameTable(
                name: "outbox_state",
                newName: "mt_outbox_state");

            migrationBuilder.RenameTable(
                name: "outbox_message",
                newName: "mt_outbox_message");

            migrationBuilder.RenameTable(
                name: "inbox_state",
                newName: "mt_inbox_state");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "seat_holds",
                newName: "customer_id");

            migrationBuilder.RenameIndex(
                name: "ix_seat_holds_event_id_user_id",
                table: "seat_holds",
                newName: "ix_seat_holds_event_id_customer_id");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_state_created",
                table: "mt_outbox_state",
                newName: "ix_mt_outbox_state_created");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_message_outbox_id_sequence_number",
                table: "mt_outbox_message",
                newName: "ix_mt_outbox_message_outbox_id_sequence_number");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_message_inbox_message_id_inbox_consumer_id_sequence_",
                table: "mt_outbox_message",
                newName: "ix_mt_outbox_message_inbox_message_id_inbox_consumer_id_sequen");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_message_expiration_time",
                table: "mt_outbox_message",
                newName: "ix_mt_outbox_message_expiration_time");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_message_enqueue_time",
                table: "mt_outbox_message",
                newName: "ix_mt_outbox_message_enqueue_time");

            migrationBuilder.RenameIndex(
                name: "ix_inbox_state_delivered",
                table: "mt_inbox_state",
                newName: "ix_mt_inbox_state_delivered");

            // Order'dan önceki REST tutma testlerinden (22 Ekim) kalan, siparişe bağlı olmayan kayıtlar. order_id zorunlu
            // ve benzersiz olunca hepsine aynı boş değer yazılamazdı.
            migrationBuilder.Sql("DELETE FROM seat_holds WHERE order_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "seat_holds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_mt_outbox_state",
                table: "mt_outbox_state",
                column: "outbox_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mt_outbox_message",
                table: "mt_outbox_message",
                column: "sequence_number");

            migrationBuilder.AddPrimaryKey(
                name: "pk_mt_inbox_state",
                table: "mt_inbox_state",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_mt_outbox_message_mt_inbox_state_inbox_message_id_inbox_con",
                table: "mt_outbox_message",
                columns: new[] { "inbox_message_id", "inbox_consumer_id" },
                principalTable: "mt_inbox_state",
                principalColumns: new[] { "message_id", "consumer_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_mt_outbox_message_outbox_state_outbox_id",
                table: "mt_outbox_message",
                column: "outbox_id",
                principalTable: "mt_outbox_state",
                principalColumn: "outbox_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_mt_outbox_message_mt_inbox_state_inbox_message_id_inbox_con",
                table: "mt_outbox_message");

            migrationBuilder.DropForeignKey(
                name: "fk_mt_outbox_message_outbox_state_outbox_id",
                table: "mt_outbox_message");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mt_outbox_state",
                table: "mt_outbox_state");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mt_outbox_message",
                table: "mt_outbox_message");

            migrationBuilder.DropPrimaryKey(
                name: "pk_mt_inbox_state",
                table: "mt_inbox_state");

            migrationBuilder.RenameTable(
                name: "mt_outbox_state",
                newName: "outbox_state");

            migrationBuilder.RenameTable(
                name: "mt_outbox_message",
                newName: "outbox_message");

            migrationBuilder.RenameTable(
                name: "mt_inbox_state",
                newName: "inbox_state");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "seat_holds",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_seat_holds_event_id_customer_id",
                table: "seat_holds",
                newName: "ix_seat_holds_event_id_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_mt_outbox_state_created",
                table: "outbox_state",
                newName: "ix_outbox_state_created");

            migrationBuilder.RenameIndex(
                name: "ix_mt_outbox_message_outbox_id_sequence_number",
                table: "outbox_message",
                newName: "ix_outbox_message_outbox_id_sequence_number");

            migrationBuilder.RenameIndex(
                name: "ix_mt_outbox_message_inbox_message_id_inbox_consumer_id_sequen",
                table: "outbox_message",
                newName: "ix_outbox_message_inbox_message_id_inbox_consumer_id_sequence_");

            migrationBuilder.RenameIndex(
                name: "ix_mt_outbox_message_expiration_time",
                table: "outbox_message",
                newName: "ix_outbox_message_expiration_time");

            migrationBuilder.RenameIndex(
                name: "ix_mt_outbox_message_enqueue_time",
                table: "outbox_message",
                newName: "ix_outbox_message_enqueue_time");

            migrationBuilder.RenameIndex(
                name: "ix_mt_inbox_state_delivered",
                table: "inbox_state",
                newName: "ix_inbox_state_delivered");

            migrationBuilder.AlterColumn<Guid>(
                name: "order_id",
                table: "seat_holds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_outbox_state",
                table: "outbox_state",
                column: "outbox_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_outbox_message",
                table: "outbox_message",
                column: "sequence_number");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inbox_state",
                table: "inbox_state",
                column: "id");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_dead_lettered = table.Column<bool>(type: "boolean", nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trace_parent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at",
                filter: "processed_at IS NULL AND NOT is_dead_lettered");

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_message_inbox_state_inbox_message_id_inbox_consumer_",
                table: "outbox_message",
                columns: new[] { "inbox_message_id", "inbox_consumer_id" },
                principalTable: "inbox_state",
                principalColumns: new[] { "message_id", "consumer_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_message_outbox_state_outbox_id",
                table: "outbox_message",
                column: "outbox_id",
                principalTable: "outbox_state",
                principalColumn: "outbox_id");
        }
    }
}
