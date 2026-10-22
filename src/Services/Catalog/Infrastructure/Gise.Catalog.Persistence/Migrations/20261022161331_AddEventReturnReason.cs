using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReturnReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "return_reason",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "return_reason",
                table: "events");
        }
    }
}
