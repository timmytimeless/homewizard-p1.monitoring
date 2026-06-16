using System.ComponentModel.DataAnnotations;

namespace aiterate.energy.web.Models.Persistence;

public class HomeWizardQuarterHourAggregate
{
    [Key]
    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public int SampleCount { get; set; }

    public bool IsReliable { get; set; } = true;

    public decimal FirstEnergyImportKwh { get; set; }

    public decimal LastEnergyImportKwh { get; set; }

    public decimal EnergyImportKwh { get; set; }

    public decimal FirstEnergyExportKwh { get; set; }

    public decimal LastEnergyExportKwh { get; set; }

    public decimal EnergyExportKwh { get; set; }

    public decimal FirstEnergyImportT1Kwh { get; set; }

    public decimal LastEnergyImportT1Kwh { get; set; }

    public decimal EnergyImportT1Kwh { get; set; }

    public decimal FirstEnergyImportT2Kwh { get; set; }

    public decimal LastEnergyImportT2Kwh { get; set; }

    public decimal EnergyImportT2Kwh { get; set; }

    public decimal FirstEnergyExportT1Kwh { get; set; }

    public decimal LastEnergyExportT1Kwh { get; set; }

    public decimal EnergyExportT1Kwh { get; set; }

    public decimal FirstEnergyExportT2Kwh { get; set; }

    public decimal LastEnergyExportT2Kwh { get; set; }

    public decimal EnergyExportT2Kwh { get; set; }

    public decimal AveragePowerW { get; set; }

    public int MinimumPowerW { get; set; }

    public int MaximumPowerW { get; set; }

    public decimal AveragePowerL1W { get; set; }

    public decimal AveragePowerL2W { get; set; }

    public decimal AveragePowerL3W { get; set; }

    public decimal AverageVoltageL1V { get; set; }

    public decimal AverageVoltageL2V { get; set; }

    public decimal AverageVoltageL3V { get; set; }

    public decimal AverageCurrentL1A { get; set; }

    public decimal AverageCurrentL2A { get; set; }

    public decimal AverageCurrentL3A { get; set; }

    public int FirstVoltageSagL1Count { get; set; }

    public int LastVoltageSagL1Count { get; set; }

    public int VoltageSagL1Count { get; set; }

    public int FirstVoltageSagL2Count { get; set; }

    public int LastVoltageSagL2Count { get; set; }

    public int VoltageSagL2Count { get; set; }

    public int FirstVoltageSagL3Count { get; set; }

    public int LastVoltageSagL3Count { get; set; }

    public int VoltageSagL3Count { get; set; }

    public int FirstVoltageSwellL1Count { get; set; }

    public int LastVoltageSwellL1Count { get; set; }

    public int VoltageSwellL1Count { get; set; }

    public int FirstVoltageSwellL2Count { get; set; }

    public int LastVoltageSwellL2Count { get; set; }

    public int VoltageSwellL2Count { get; set; }

    public int FirstVoltageSwellL3Count { get; set; }

    public int LastVoltageSwellL3Count { get; set; }

    public int VoltageSwellL3Count { get; set; }

    public int FirstAnyPowerFailCount { get; set; }

    public int LastAnyPowerFailCount { get; set; }

    public int AnyPowerFailCount { get; set; }

    public int FirstLongPowerFailCount { get; set; }

    public int LastLongPowerFailCount { get; set; }

    public int LongPowerFailCount { get; set; }
}
