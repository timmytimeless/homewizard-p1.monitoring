using aiterate.energy.web.Models;
using aiterate.energy.web.Models.Persistence;

namespace aiterate.energy.web.Services.HomeWizard;

public static class HomeWizardAggregateUpdater
{
    public static HomeWizardQuarterHourAggregate Create(DateTime periodStart, int bucketMinutes, DateTime measuredAt, HomeWizardMeasurement measurement)
    {
        var aggregate = new HomeWizardQuarterHourAggregate
        {
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMinutes(bucketMinutes),
            FirstSeenAt = measuredAt,
            LastSeenAt = measuredAt,
            SampleCount = 1,
            FirstEnergyImportKwh = ToDecimal(measurement.EnergyImportKwh),
            FirstEnergyExportKwh = ToDecimal(measurement.EnergyExportKwh),
            FirstEnergyImportT1Kwh = ToDecimal(measurement.EnergyImportT1Kwh),
            FirstEnergyImportT2Kwh = ToDecimal(measurement.EnergyImportT2Kwh),
            FirstEnergyExportT1Kwh = ToDecimal(measurement.EnergyExportT1Kwh),
            FirstEnergyExportT2Kwh = ToDecimal(measurement.EnergyExportT2Kwh),
            MinimumPowerW = measurement.PowerW,
            MaximumPowerW = measurement.PowerW,
            FirstVoltageSagL1Count = measurement.VoltageSagL1Count,
            FirstVoltageSagL2Count = measurement.VoltageSagL2Count,
            FirstVoltageSagL3Count = measurement.VoltageSagL3Count,
            FirstVoltageSwellL1Count = measurement.VoltageSwellL1Count,
            FirstVoltageSwellL2Count = measurement.VoltageSwellL2Count,
            FirstVoltageSwellL3Count = measurement.VoltageSwellL3Count,
            FirstAnyPowerFailCount = measurement.AnyPowerFailCount,
            FirstLongPowerFailCount = measurement.LongPowerFailCount
        };

        ApplyLatestValues(aggregate, measurement);
        ApplyAverages(aggregate, measurement);
        return aggregate;
    }

    public static void Update(HomeWizardQuarterHourAggregate aggregate, DateTime measuredAt, HomeWizardMeasurement measurement)
    {
        aggregate.LastSeenAt = measuredAt;
        aggregate.SampleCount += 1;
        aggregate.MinimumPowerW = Math.Min(aggregate.MinimumPowerW, measurement.PowerW);
        aggregate.MaximumPowerW = Math.Max(aggregate.MaximumPowerW, measurement.PowerW);

        ApplyLatestValues(aggregate, measurement);
        ApplyAverages(aggregate, measurement);
    }

    private static void ApplyLatestValues(HomeWizardQuarterHourAggregate aggregate, HomeWizardMeasurement measurement)
    {
        aggregate.LastEnergyImportKwh = ToDecimal(measurement.EnergyImportKwh);
        aggregate.LastEnergyExportKwh = ToDecimal(measurement.EnergyExportKwh);
        aggregate.LastEnergyImportT1Kwh = ToDecimal(measurement.EnergyImportT1Kwh);
        aggregate.LastEnergyImportT2Kwh = ToDecimal(measurement.EnergyImportT2Kwh);
        aggregate.LastEnergyExportT1Kwh = ToDecimal(measurement.EnergyExportT1Kwh);
        aggregate.LastEnergyExportT2Kwh = ToDecimal(measurement.EnergyExportT2Kwh);

        aggregate.EnergyImportKwh = Delta(aggregate.FirstEnergyImportKwh, aggregate.LastEnergyImportKwh, aggregate);
        aggregate.EnergyExportKwh = Delta(aggregate.FirstEnergyExportKwh, aggregate.LastEnergyExportKwh, aggregate);
        aggregate.EnergyImportT1Kwh = Delta(aggregate.FirstEnergyImportT1Kwh, aggregate.LastEnergyImportT1Kwh, aggregate);
        aggregate.EnergyImportT2Kwh = Delta(aggregate.FirstEnergyImportT2Kwh, aggregate.LastEnergyImportT2Kwh, aggregate);
        aggregate.EnergyExportT1Kwh = Delta(aggregate.FirstEnergyExportT1Kwh, aggregate.LastEnergyExportT1Kwh, aggregate);
        aggregate.EnergyExportT2Kwh = Delta(aggregate.FirstEnergyExportT2Kwh, aggregate.LastEnergyExportT2Kwh, aggregate);

        aggregate.LastVoltageSagL1Count = measurement.VoltageSagL1Count;
        aggregate.LastVoltageSagL2Count = measurement.VoltageSagL2Count;
        aggregate.LastVoltageSagL3Count = measurement.VoltageSagL3Count;
        aggregate.LastVoltageSwellL1Count = measurement.VoltageSwellL1Count;
        aggregate.LastVoltageSwellL2Count = measurement.VoltageSwellL2Count;
        aggregate.LastVoltageSwellL3Count = measurement.VoltageSwellL3Count;
        aggregate.LastAnyPowerFailCount = measurement.AnyPowerFailCount;
        aggregate.LastLongPowerFailCount = measurement.LongPowerFailCount;

        aggregate.VoltageSagL1Count = CountDelta(aggregate.FirstVoltageSagL1Count, aggregate.LastVoltageSagL1Count, aggregate);
        aggregate.VoltageSagL2Count = CountDelta(aggregate.FirstVoltageSagL2Count, aggregate.LastVoltageSagL2Count, aggregate);
        aggregate.VoltageSagL3Count = CountDelta(aggregate.FirstVoltageSagL3Count, aggregate.LastVoltageSagL3Count, aggregate);
        aggregate.VoltageSwellL1Count = CountDelta(aggregate.FirstVoltageSwellL1Count, aggregate.LastVoltageSwellL1Count, aggregate);
        aggregate.VoltageSwellL2Count = CountDelta(aggregate.FirstVoltageSwellL2Count, aggregate.LastVoltageSwellL2Count, aggregate);
        aggregate.VoltageSwellL3Count = CountDelta(aggregate.FirstVoltageSwellL3Count, aggregate.LastVoltageSwellL3Count, aggregate);
        aggregate.AnyPowerFailCount = CountDelta(aggregate.FirstAnyPowerFailCount, aggregate.LastAnyPowerFailCount, aggregate);
        aggregate.LongPowerFailCount = CountDelta(aggregate.FirstLongPowerFailCount, aggregate.LastLongPowerFailCount, aggregate);
    }

    private static void ApplyAverages(HomeWizardQuarterHourAggregate aggregate, HomeWizardMeasurement measurement)
    {
        aggregate.AveragePowerW = RollingAverage(aggregate.AveragePowerW, measurement.PowerW, aggregate.SampleCount);
        aggregate.AveragePowerL1W = RollingAverage(aggregate.AveragePowerL1W, measurement.PowerL1W, aggregate.SampleCount);
        aggregate.AveragePowerL2W = RollingAverage(aggregate.AveragePowerL2W, measurement.PowerL2W, aggregate.SampleCount);
        aggregate.AveragePowerL3W = RollingAverage(aggregate.AveragePowerL3W, measurement.PowerL3W, aggregate.SampleCount);
        aggregate.AverageVoltageL1V = RollingAverage(aggregate.AverageVoltageL1V, ToDecimal(measurement.VoltageL1V), aggregate.SampleCount);
        aggregate.AverageVoltageL2V = RollingAverage(aggregate.AverageVoltageL2V, ToDecimal(measurement.VoltageL2V), aggregate.SampleCount);
        aggregate.AverageVoltageL3V = RollingAverage(aggregate.AverageVoltageL3V, ToDecimal(measurement.VoltageL3V), aggregate.SampleCount);
        aggregate.AverageCurrentL1A = RollingAverage(aggregate.AverageCurrentL1A, ToDecimal(measurement.CurrentL1A), aggregate.SampleCount);
        aggregate.AverageCurrentL2A = RollingAverage(aggregate.AverageCurrentL2A, ToDecimal(measurement.CurrentL2A), aggregate.SampleCount);
        aggregate.AverageCurrentL3A = RollingAverage(aggregate.AverageCurrentL3A, ToDecimal(measurement.CurrentL3A), aggregate.SampleCount);
    }

    private static decimal RollingAverage(decimal currentAverage, decimal nextValue, int sampleCount)
    {
        if (sampleCount <= 1)
        {
            return nextValue;
        }

        return currentAverage + ((nextValue - currentAverage) / sampleCount);
    }

    private static decimal Delta(decimal first, decimal last, HomeWizardQuarterHourAggregate aggregate)
    {
        if (last >= first)
        {
            return last - first;
        }

        aggregate.IsReliable = false;
        return 0;
    }

    private static int CountDelta(int first, int last, HomeWizardQuarterHourAggregate aggregate)
    {
        if (last >= first)
        {
            return last - first;
        }

        aggregate.IsReliable = false;
        return 0;
    }

    private static decimal ToDecimal(double value) => Convert.ToDecimal(value);
}
