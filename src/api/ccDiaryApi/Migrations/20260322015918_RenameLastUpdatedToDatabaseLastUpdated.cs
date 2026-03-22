using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ccDiaryApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameLastUpdatedToDatabaseLastUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "AppInfo",
                newName: "DatabaseLastUpdated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DatabaseLastUpdated",
                table: "AppInfo",
                newName: "LastUpdated");
        }
    }
}
