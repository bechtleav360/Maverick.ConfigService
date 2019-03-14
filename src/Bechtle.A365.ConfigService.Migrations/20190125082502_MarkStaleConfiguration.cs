using Microsoft.EntityFrameworkCore.Migrations;

namespace Bechtle.A365.ConfigService.Migrations
{
    public partial class MarkStaleConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UpToDate",
                table: "ProjectedConfigurations",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpToDate",
                table: "ProjectedConfigurations");
        }
    }
}
