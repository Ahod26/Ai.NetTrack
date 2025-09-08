using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsItemToContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsItem",
                table: "NewsItem");

            migrationBuilder.RenameTable(
                name: "NewsItem",
                newName: "NewsItems");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItem_SourceType_PublishedDate",
                table: "NewsItems",
                newName: "IX_NewsItems_SourceType_PublishedDate");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItem_SourceType",
                table: "NewsItems",
                newName: "IX_NewsItems_SourceType");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItem_PublishedDate",
                table: "NewsItems",
                newName: "IX_NewsItems_PublishedDate");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsItems",
                table: "NewsItems",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NewsItems",
                table: "NewsItems");

            migrationBuilder.RenameTable(
                name: "NewsItems",
                newName: "NewsItem");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItems_SourceType_PublishedDate",
                table: "NewsItem",
                newName: "IX_NewsItem_SourceType_PublishedDate");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItems_SourceType",
                table: "NewsItem",
                newName: "IX_NewsItem_SourceType");

            migrationBuilder.RenameIndex(
                name: "IX_NewsItems_PublishedDate",
                table: "NewsItem",
                newName: "IX_NewsItem_PublishedDate");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NewsItem",
                table: "NewsItem",
                column: "Id");
        }
    }
}
