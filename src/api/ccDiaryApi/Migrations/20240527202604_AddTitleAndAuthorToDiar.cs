// <copyright file="20240527202604_AddTitleAndAuthorToDiar.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

#nullable disable

namespace ccDiaryApi.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddTitleAndAuthorToDiar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Diary");

            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Diary",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Diary",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateTable(
                name: "DiaryEntry",
                columns: table => new
                {
                    DiaryEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Entry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryEntry", x => x.DiaryEntryId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiaryEntry");

            migrationBuilder.DropColumn(
                name: "Author",
                table: "Diary");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Diary");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Diary",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty);
        }
    }
}
