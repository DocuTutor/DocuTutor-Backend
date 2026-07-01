using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocuTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkDocumentsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Documents",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UserId",
                table: "Documents",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AspNetUsers_UserId",
                table: "Documents",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_UserId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UserId",
                table: "Documents");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Documents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);
        }
    }
}
