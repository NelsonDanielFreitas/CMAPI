using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvariaAtribuicaoId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AvariaId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseReason",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponseStatus",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetStatusId",
                table: "Assets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AssetStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AvariaAtribuicaoId",
                table: "Notifications",
                column: "AvariaAtribuicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AvariaId",
                table: "Notifications",
                column: "AvariaId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetStatusId",
                table: "Assets",
                column: "AssetStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_AssetStatuses_AssetStatusId",
                table: "Assets",
                column: "AssetStatusId",
                principalTable: "AssetStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AvariaAtribuicoes_AvariaAtribuicaoId",
                table: "Notifications",
                column: "AvariaAtribuicaoId",
                principalTable: "AvariaAtribuicoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Avaria_AvariaId",
                table: "Notifications",
                column: "AvariaId",
                principalTable: "Avaria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_AssetStatuses_AssetStatusId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AvariaAtribuicoes_AvariaAtribuicaoId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Avaria_AvariaId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "AssetStatuses");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_AvariaAtribuicaoId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_AvariaId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Assets_AssetStatusId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "AvariaAtribuicaoId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AvariaId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ResponseAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ResponseReason",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ResponseStatus",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AssetStatusId",
                table: "Assets");
        }
    }
}
