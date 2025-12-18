using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devjamin.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceContentWithSemanticHTMLAndQuillDelta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add the new QuillDelta column
            migrationBuilder.AddColumn<string>(
                name: "QuillDelta",
                table: "BlogPosts",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Rename Content to SemanticHTML (this preserves the existing semantic HTML data)
            migrationBuilder.RenameColumn(
                name: "Content",
                table: "BlogPosts",
                newName: "SemanticHTML");

            // Note: QuillContent data will be lost as we cannot automatically convert HTML to Quill Delta
            // Users will need to re-edit their posts to generate new QuillDelta data
            migrationBuilder.DropColumn(
                name: "QuillContent",
                table: "BlogPosts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuillDelta",
                table: "BlogPosts");

            migrationBuilder.RenameColumn(
                name: "SemanticHTML",
                table: "BlogPosts",
                newName: "Content");

            migrationBuilder.AddColumn<string>(
                name: "QuillContent",
                table: "BlogPosts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
