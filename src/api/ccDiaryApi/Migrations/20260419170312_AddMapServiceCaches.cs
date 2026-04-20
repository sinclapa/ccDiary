#nullable disable

namespace ccDiaryApi.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddMapServiceCaches : Migration
    {
        private static readonly string[] MapTileCacheColumns = { "Source", "Z", "X", "Y" };

        private static readonly string[] RoutingCacheColumns = { "FromLat", "FromLon", "ToLat", "ToLon", "Profile" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeocodingCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lat = table.Column<double>(type: "float", nullable: false),
                    Lon = table.Column<double>(type: "float", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeocodingCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MapTileCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Z = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    TileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapTileCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoutingCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromLat = table.Column<double>(type: "float", nullable: false),
                    FromLon = table.Column<double>(type: "float", nullable: false),
                    ToLat = table.Column<double>(type: "float", nullable: false),
                    ToLon = table.Column<double>(type: "float", nullable: false),
                    Profile = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RouteCoords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeocodingCache_Query",
                table: "GeocodingCache",
                column: "Query",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapTileCache_Source_Z_X_Y",
                table: "MapTileCache",
                columns: MapTileCacheColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingCache_FromLat_FromLon_ToLat_ToLon_Profile",
                table: "RoutingCache",
                columns: RoutingCacheColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeocodingCache");

            migrationBuilder.DropTable(
                name: "MapTileCache");

            migrationBuilder.DropTable(
                name: "RoutingCache");
        }
    }
}
