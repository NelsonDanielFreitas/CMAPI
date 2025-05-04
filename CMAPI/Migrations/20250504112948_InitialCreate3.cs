using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Assets_AssetId",
                table: "Avaria");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Users_TechnicianId",
                table: "Avaria");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Users_UserId",
                table: "Avaria");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Avaria",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "TechnicianId",
                table: "Avaria",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssetId",
                table: "Avaria",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Assets_AssetId",
                table: "Avaria",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Users_TechnicianId",
                table: "Avaria",
                column: "TechnicianId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Users_UserId",
                table: "Avaria",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Assets_AssetId",
                table: "Avaria");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Users_TechnicianId",
                table: "Avaria");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaria_Users_UserId",
                table: "Avaria");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Avaria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TechnicianId",
                table: "Avaria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AssetId",
                table: "Avaria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Assets_AssetId",
                table: "Avaria",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Users_TechnicianId",
                table: "Avaria",
                column: "TechnicianId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Avaria_Users_UserId",
                table: "Avaria",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
