using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gise.AuditLog.Worker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kafka_topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kafka_partition = table.Column<int>(type: "integer", nullable: false),
                    kafka_offset = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    logged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    service = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_roles = table.Column<string[]>(type: "text[]", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    client_ip = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    targets = table.Column<string>(type: "jsonb", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_action_occurred_at",
                table: "audit_records",
                columns: new[] { "action", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_actor_id_occurred_at",
                table: "audit_records",
                columns: new[] { "actor_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_kafka_topic_kafka_partition_kafka_offset",
                table: "audit_records",
                columns: new[] { "kafka_topic", "kafka_partition", "kafka_offset" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_records_occurred_at",
                table: "audit_records",
                column: "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_records");
        }
    }
}
