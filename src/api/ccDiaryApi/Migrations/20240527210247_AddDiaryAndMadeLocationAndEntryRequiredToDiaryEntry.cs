// <copyright file="20240527210247_AddDiaryAndMadeLocationAndEntryRequiredToDiaryEntry.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

#nullable disable

namespace ccDiaryApi.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <inheritdoc />
    public partial class AddDiaryAndMadeLocationAndEntryRequiredToDiaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Entry",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntry_DiaryId",
                table: "DiaryEntry",
                column: "DiaryId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiaryEntry_Diary_DiaryId",
                table: "DiaryEntry",
                column: "DiaryId",
                principalTable: "Diary",
                principalColumn: "DiaryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiaryEntry_Diary_DiaryId",
                table: "DiaryEntry");

            migrationBuilder.DropIndex(
                name: "IX_DiaryEntry_DiaryId",
                table: "DiaryEntry");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Entry",
                table: "DiaryEntry",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
