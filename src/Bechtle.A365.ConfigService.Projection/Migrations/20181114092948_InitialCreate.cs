using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Projection.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigEnvironments",
                columns: table => new
                {
                    Category = table.Column<string>(nullable: true),
                    DefaultEnvironment = table.Column<bool>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigEnvironments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    LatestEvent = table.Column<long>(nullable: true),
                    LastActiveConfigurationId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Structures",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Structures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigEnvironmentKey",
                columns: table => new
                {
                    ConfigEnvironmentId = table.Column<Guid>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigEnvironmentKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigEnvironmentKey_ConfigEnvironments_ConfigEnvironmentId",
                        column: x => x.ConfigEnvironmentId,
                        principalTable: "ConfigEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectedConfigurations",
                columns: table => new
                {
                    ConfigEnvironmentId = table.Column<Guid>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    StructureId = table.Column<Guid>(nullable: false),
                    StructureVersion = table.Column<int>(nullable: false),
                    ConfigurationJson = table.Column<string>(nullable: true),
                    ValidFrom = table.Column<DateTime>(nullable: true),
                    ValidTo = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectedConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectedConfigurations_ConfigEnvironments_ConfigEnvironmentId",
                        column: x => x.ConfigEnvironmentId,
                        principalTable: "ConfigEnvironments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectedConfigurations_Structures_StructureId",
                        column: x => x.StructureId,
                        principalTable: "Structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructureKey",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Key = table.Column<string>(nullable: true),
                    StructureId = table.Column<Guid>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureKey_Structures_StructureId",
                        column: x => x.StructureId,
                        principalTable: "Structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StructureVariable",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Key = table.Column<string>(nullable: true),
                    StructureId = table.Column<Guid>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureVariable_Structures_StructureId",
                        column: x => x.StructureId,
                        principalTable: "Structures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectedConfigurationKey",
                columns: table => new
                {
                    ProjectedConfigurationId = table.Column<Guid>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectedConfigurationKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectedConfigurationKey_ProjectedConfigurations_ProjectedConfigurationId",
                        column: x => x.ProjectedConfigurationId,
                        principalTable: "ProjectedConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigEnvironmentKey_ConfigEnvironmentId",
                table: "ConfigEnvironmentKey",
                column: "ConfigEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectedConfigurationKey_ProjectedConfigurationId",
                table: "ProjectedConfigurationKey",
                column: "ProjectedConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectedConfigurations_ConfigEnvironmentId",
                table: "ProjectedConfigurations",
                column: "ConfigEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectedConfigurations_StructureId",
                table: "ProjectedConfigurations",
                column: "StructureId");

            migrationBuilder.CreateIndex(
                name: "IX_StructureKey_StructureId",
                table: "StructureKey",
                column: "StructureId");

            migrationBuilder.CreateIndex(
                name: "IX_StructureVariable_StructureId",
                table: "StructureVariable",
                column: "StructureId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigEnvironmentKey");

            migrationBuilder.DropTable(
                name: "Metadata");

            migrationBuilder.DropTable(
                name: "ProjectedConfigurationKey");

            migrationBuilder.DropTable(
                name: "StructureKey");

            migrationBuilder.DropTable(
                name: "StructureVariable");

            migrationBuilder.DropTable(
                name: "ProjectedConfigurations");

            migrationBuilder.DropTable(
                name: "ConfigEnvironments");

            migrationBuilder.DropTable(
                name: "Structures");
        }
    }
}
