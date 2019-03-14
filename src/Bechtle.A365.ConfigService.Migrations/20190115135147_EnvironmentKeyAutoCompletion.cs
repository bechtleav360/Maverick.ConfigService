using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Migrations
{
    public partial class EnvironmentKeyAutoCompletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoCompletePaths",
                columns: table => new
                {
                    ConfigEnvironmentId = table.Column<Guid>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    ParentId = table.Column<Guid>(nullable: true),
                    Path = table.Column<string>(nullable: true),
                    FullPath = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoCompletePaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutoCompletePaths_ConfigEnvironments_ConfigEnvironmentId",
                        column: x => x.ConfigEnvironmentId,
                        principalTable: "ConfigEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AutoCompletePaths_AutoCompletePaths_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AutoCompletePaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoCompletePaths_ConfigEnvironmentId",
                table: "AutoCompletePaths",
                column: "ConfigEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoCompletePaths_ParentId",
                table: "AutoCompletePaths",
                column: "ParentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoCompletePaths");
        }
    }
}
