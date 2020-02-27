using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Migrations.Migrations.Postgres
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mavconfig");

            migrationBuilder.CreateTable(
                name: "Snapshots",
                schema: "mavconfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DataType = table.Column<string>(nullable: true),
                    Identifier = table.Column<string>(nullable: true),
                    JsonData = table.Column<string>(nullable: true),
                    MetaVersion = table.Column<long>(nullable: false),
                    Version = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Snapshots",
                schema: "mavconfig");
        }
    }
}
