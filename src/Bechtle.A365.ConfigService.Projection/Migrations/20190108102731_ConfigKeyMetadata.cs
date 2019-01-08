using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Projection.Migrations
{
    public partial class ConfigKeyMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ConfigEnvironmentKey",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ConfigEnvironmentKey",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ConfigEnvironmentKey");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ConfigEnvironmentKey");
        }
    }
}
