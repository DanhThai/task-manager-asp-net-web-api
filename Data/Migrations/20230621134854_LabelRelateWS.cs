using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Data.Migrations
{
    public partial class LabelRelateWS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels");

            migrationBuilder.AlterColumn<int>(
                name: "WorkspaceId",
                table: "Labels",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels");

            migrationBuilder.AlterColumn<int>(
                name: "WorkspaceId",
                table: "Labels",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Labels_Workspaces_WorkspaceId",
                table: "Labels",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id");
        }
    }
}
