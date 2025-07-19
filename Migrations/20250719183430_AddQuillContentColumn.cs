using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devjamin.Migrations
{
    /// <inheritdoc />
    public partial class AddQuillContentColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuillContent",
                table: "BlogPosts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuillContent",
                table: "BlogPosts");
        }
    }
}
