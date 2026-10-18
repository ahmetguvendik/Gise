using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "outbox_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "outbox_messages");
        }
    }
}
