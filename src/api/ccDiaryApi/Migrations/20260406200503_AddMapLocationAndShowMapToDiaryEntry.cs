using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ccDiaryApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMapLocationAndShowMapToDiaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MapLocation",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMap",
                table: "DiaryEntry",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapLocation",
                table: "DiaryEntry");

            migrationBuilder.DropColumn(
                name: "ShowMap",
                table: "DiaryEntry");
        }
    }
}
