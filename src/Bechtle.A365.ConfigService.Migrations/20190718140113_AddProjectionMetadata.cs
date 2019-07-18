using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Migrations
{
    public partial class AddProjectionMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                table: "ProjectedConfigurationKey");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectedConfigurations_ConfigEnvironments_ConfigEnvironmentId",
                table: "ProjectedConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_UsedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                table: "UsedConfigurationKey");

            migrationBuilder.CreateTable(
                name: "ProjectedEventMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Index = table.Column<long>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    ProjectedSuccessfully = table.Column<bool>(nullable: false),
                    Start = table.Column<DateTime>(nullable: false),
                    End = table.Column<DateTime>(nullable: false),
                    Changes = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectedEventMetadata", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectedConfigurationKey_ProjectedConfigurations_Projected~",
                table: "ProjectedConfigurationKey",
                column: "ProjectedConfigurationId",
                principalTable: "ProjectedConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectedConfigurations_ConfigEnvironments_ConfigEnvironmen~",
                table: "ProjectedConfigurations",
                column: "ConfigEnvironmentId",
                principalTable: "ConfigEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsedConfigurationKey_ProjectedConfigurations_ProjectedConfi~",
                table: "UsedConfigurationKey",
                column: "ProjectedConfigurationId",
                principalTable: "ProjectedConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectedConfigurationKey_ProjectedConfigurations_Projected~",
                table: "ProjectedConfigurationKey");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectedConfigurations_ConfigEnvironments_ConfigEnvironmen~",
                table: "ProjectedConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_UsedConfigurationKey_ProjectedConfigurations_ProjectedConfi~",
                table: "UsedConfigurationKey");

            migrationBuilder.DropTable(
                name: "ProjectedEventMetadata");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                table: "ProjectedConfigurationKey",
                column: "ProjectedConfigurationId",
                principalTable: "ProjectedConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectedConfigurations_ConfigEnvironments_ConfigEnvironmentId",
                table: "ProjectedConfigurations",
                column: "ConfigEnvironmentId",
                principalTable: "ConfigEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UsedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                table: "UsedConfigurationKey",
                column: "ProjectedConfigurationId",
                principalTable: "ProjectedConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
