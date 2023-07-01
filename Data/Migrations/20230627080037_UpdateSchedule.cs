using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Data.Migrations
{
    public partial class UpdateSchedule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Schedules",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Schedules",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Schedules",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "Schedules",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_CreatorId",
                table: "Schedules",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_AspNetUsers_CreatorId",
                table: "Schedules",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_AspNetUsers_CreatorId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_CreatorId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "Schedules");

            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                table: "Schedules",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Schedules",
                newName: "Content");
        }
    }
}
