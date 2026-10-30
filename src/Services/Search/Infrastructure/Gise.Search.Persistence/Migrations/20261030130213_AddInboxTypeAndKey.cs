using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Search.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInboxTypeAndKey : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "key",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "type",
                table: "inbox_messages");
        }
    }
}
