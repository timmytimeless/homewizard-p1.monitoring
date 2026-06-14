using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aiterate.energy.web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeWizardP1TokenToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeWizardP1Token",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeWizardP1Token",
                table: "AspNetUsers");
        }
    }
}
