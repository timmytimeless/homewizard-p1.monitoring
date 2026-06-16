using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aiterate.energy.web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeWizardQuarterHourAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeWizardQuarterHourAggregates",
                columns: table => new
                {
                    PeriodStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SampleCount = table.Column<int>(type: "int", nullable: false),
                    IsReliable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FirstEnergyImportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyImportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyImportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstEnergyExportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyExportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyExportKwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstEnergyImportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyImportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyImportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstEnergyImportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyImportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyImportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstEnergyExportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyExportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyExportT1Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstEnergyExportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    LastEnergyExportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    EnergyExportT2Kwh = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AveragePowerW = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    MinimumPowerW = table.Column<int>(type: "int", nullable: false),
                    MaximumPowerW = table.Column<int>(type: "int", nullable: false),
                    AveragePowerL1W = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AveragePowerL2W = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AveragePowerL3W = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageVoltageL1V = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageVoltageL2V = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageVoltageL3V = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageCurrentL1A = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageCurrentL2A = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    AverageCurrentL3A = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    FirstVoltageSagL1Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSagL1Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSagL1Count = table.Column<int>(type: "int", nullable: false),
                    FirstVoltageSagL2Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSagL2Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSagL2Count = table.Column<int>(type: "int", nullable: false),
                    FirstVoltageSagL3Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSagL3Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSagL3Count = table.Column<int>(type: "int", nullable: false),
                    FirstVoltageSwellL1Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSwellL1Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSwellL1Count = table.Column<int>(type: "int", nullable: false),
                    FirstVoltageSwellL2Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSwellL2Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSwellL2Count = table.Column<int>(type: "int", nullable: false),
                    FirstVoltageSwellL3Count = table.Column<int>(type: "int", nullable: false),
                    LastVoltageSwellL3Count = table.Column<int>(type: "int", nullable: false),
                    VoltageSwellL3Count = table.Column<int>(type: "int", nullable: false),
                    FirstAnyPowerFailCount = table.Column<int>(type: "int", nullable: false),
                    LastAnyPowerFailCount = table.Column<int>(type: "int", nullable: false),
                    AnyPowerFailCount = table.Column<int>(type: "int", nullable: false),
                    FirstLongPowerFailCount = table.Column<int>(type: "int", nullable: false),
                    LastLongPowerFailCount = table.Column<int>(type: "int", nullable: false),
                    LongPowerFailCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeWizardQuarterHourAggregates", x => x.PeriodStart);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HomeWizardQuarterHourAggregates_PeriodEnd",
                table: "HomeWizardQuarterHourAggregates",
                column: "PeriodEnd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeWizardQuarterHourAggregates");
        }
    }
}
