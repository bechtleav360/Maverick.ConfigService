using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Migrations
{
    public partial class addintrospection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsedConfigurationKey",
                columns: table => new
                {
                    ProjectedConfigurationId = table.Column<Guid>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    Key = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsedConfigurationKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                        column: x => x.ProjectedConfigurationId,
                        principalTable: "ProjectedConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsedConfigurationKey_ProjectedConfigurationId",
                table: "UsedConfigurationKey",
                column: "ProjectedConfigurationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsedConfigurationKey");
        }
    }
}
