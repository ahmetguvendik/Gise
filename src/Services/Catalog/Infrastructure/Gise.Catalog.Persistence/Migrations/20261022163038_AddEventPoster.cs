using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gise.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPoster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "poster_key",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "poster_key",
                table: "events");
        }
    }
}
