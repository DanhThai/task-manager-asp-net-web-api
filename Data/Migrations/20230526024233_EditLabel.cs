using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Data.Migrations
{
    public partial class EditLabel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedMemberId",
                table: "Subtasks",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MemberId",
                table: "Subtasks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "Labels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_AssignedMemberId",
                table: "Subtasks",
                column: "AssignedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_WorkspaceId",
                table: "Labels",
                column: "WorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_AspNetUsers_AssignedMemberId",
                table: "Subtasks",
                column: "AssignedMemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels");

            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_AspNetUsers_AssignedMemberId",
                table: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_Subtasks_AssignedMemberId",
                table: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_Labels_WorkspaceId",
                table: "Labels");

            migrationBuilder.DropColumn(
                name: "AssignedMemberId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Labels");
        }
    }
}
