using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devjamin.Migrations
{
    /// <inheritdoc />
    public partial class isCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageFilePath",
                table: "BlogPosts");

            migrationBuilder.AddColumn<bool>(
                name: "IsCoverImage",
                table: "BlogMedia",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCoverImage",
                table: "BlogMedia");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageFilePath",
                table: "BlogPosts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
