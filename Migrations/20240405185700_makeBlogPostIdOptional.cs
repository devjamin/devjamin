using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace devjamin.Migrations
{
    /// <inheritdoc />
    public partial class makeBlogPostIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlogMedia_BlogPosts_BlogPostId",
                table: "BlogMedia");

            migrationBuilder.AlterColumn<int>(
                name: "BlogPostId",
                table: "BlogMedia",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_BlogMedia_BlogPosts_BlogPostId",
                table: "BlogMedia",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "BlogPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlogMedia_BlogPosts_BlogPostId",
                table: "BlogMedia");

            migrationBuilder.AlterColumn<int>(
                name: "BlogPostId",
                table: "BlogMedia",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BlogMedia_BlogPosts_BlogPostId",
                table: "BlogMedia",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "BlogPostId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
