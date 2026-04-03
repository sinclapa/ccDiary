// <copyright file="20260322015918_RenameLastUpdatedToDatabaseLastUpdated.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

#nullable disable

namespace ccDiaryApi.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

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
