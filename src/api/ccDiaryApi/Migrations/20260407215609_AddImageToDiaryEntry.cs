using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ccDiaryApi.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToDiaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageData",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "DiaryEntry");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "DiaryEntry");
        }
    }
}
