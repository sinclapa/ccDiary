using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ccDiaryApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAppInfoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    InformationalVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInfo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppInfo");
        }
    }
}
