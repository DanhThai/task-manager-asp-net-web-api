using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Data.Migrations
{
    public partial class updateVisitDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Background",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "VisitDate",
                table: "Workspaces");

            migrationBuilder.AddColumn<DateTime>(
                name: "VisitDate",
                table: "MemberWorkspaces",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisitDate",
                table: "MemberWorkspaces");

            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "Workspaces",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Workspaces",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "VisitDate",
                table: "Workspaces",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
