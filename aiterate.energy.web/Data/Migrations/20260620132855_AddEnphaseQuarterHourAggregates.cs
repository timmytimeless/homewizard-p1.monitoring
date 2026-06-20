using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aiterate.energy.web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnphaseQuarterHourAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnphaseQuarterHourAggregates",
                columns: table => new
                {
                    PeriodStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    IsReliable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActiveInverterCount = table.Column<int>(type: "int", nullable: false),
                    FirstLifetimeProductionWh = table.Column<long>(type: "bigint", nullable: false),
                    LastLifetimeProductionWh = table.Column<long>(type: "bigint", nullable: false),
                    EnergyProductionKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AveragePowerW = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    MinimumPowerW = table.Column<int>(type: "int", nullable: false),
                    MaximumPowerW = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnphaseQuarterHourAggregates", x => x.PeriodStart);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EnphaseQuarterHourAggregates_PeriodEnd",
                table: "EnphaseQuarterHourAggregates",
                column: "PeriodEnd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnphaseQuarterHourAggregates");
        }
    }
}
