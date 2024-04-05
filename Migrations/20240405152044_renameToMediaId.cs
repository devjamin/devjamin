using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devjamin.Migrations
{
    /// <inheritdoc />
    public partial class renameToMediaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BlogMedia",
                newName: "MediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "BlogMedia",
                newName: "Id");
        }
    }
}
