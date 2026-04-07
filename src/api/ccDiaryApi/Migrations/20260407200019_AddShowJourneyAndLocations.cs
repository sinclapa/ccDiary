using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ccDiaryApi.Migrations
{
    /// <inheritdoc />
    public partial class AddShowJourneyAndLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowJourney",
                table: "DiaryEntry",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "DiaryEntry");

            migrationBuilder.DropColumn(
                name: "ShowJourney",
                table: "DiaryEntry");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                table: "DiaryEntry");
        }
    }
}
