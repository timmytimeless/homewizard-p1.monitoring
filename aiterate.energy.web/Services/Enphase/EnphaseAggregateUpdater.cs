using aiterate.energy.web.Models;
using aiterate.energy.web.Models.Persistence;

namespace aiterate.energy.web.Services.Enphase;

public static class EnphaseAggregateUpdater
{
    public static EnphaseQuarterHourAggregate Create(DateTime periodStart, int bucketMinutes, DateTime measuredAt, EnphaseProductionReading reading)
    {
        var aggregate = new EnphaseQuarterHourAggregate
        {
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMinutes(bucketMinutes),
            FirstSeenAt = measuredAt,
            LastSeenAt = measuredAt,
            SampleCount = 1,
            ActiveInverterCount = reading.ActiveCount,
            FirstLifetimeProductionWh = reading.LifetimeProductionWh,
            MinimumPowerW = reading.PowerW,
            MaximumPowerW = reading.PowerW
        };

        ApplyLatestValues(aggregate, reading);
        ApplyAverages(aggregate, reading);
        return aggregate;
    }

    public static void Update(EnphaseQuarterHourAggregate aggregate, DateTime measuredAt, EnphaseProductionReading reading)
    {
        aggregate.LastSeenAt = measuredAt;
        aggregate.SampleCount += 1;
        aggregate.ActiveInverterCount = reading.ActiveCount;
        aggregate.MinimumPowerW = Math.Min(aggregate.MinimumPowerW, reading.PowerW);
        aggregate.MaximumPowerW = Math.Max(aggregate.MaximumPowerW, reading.PowerW);

        ApplyLatestValues(aggregate, reading);
        ApplyAverages(aggregate, reading);
    }

    private static void ApplyLatestValues(EnphaseQuarterHourAggregate aggregate, EnphaseProductionReading reading)
    {
        aggregate.LastLifetimeProductionWh = reading.LifetimeProductionWh;
        aggregate.EnergyProductionKwh = WhDeltaToKwh(
            aggregate.FirstLifetimeProductionWh,
            aggregate.LastLifetimeProductionWh,
            aggregate);
    }

    private static void ApplyAverages(EnphaseQuarterHourAggregate aggregate, EnphaseProductionReading reading)
    {
        aggregate.AveragePowerW = RollingAverage(aggregate.AveragePowerW, reading.PowerW, aggregate.SampleCount);
    }

    private static decimal RollingAverage(decimal currentAverage, decimal nextValue, int sampleCount)
    {
        if (sampleCount <= 1)
        {
            return nextValue;
        }

        return currentAverage + ((nextValue - currentAverage) / sampleCount);
    }

    private static decimal WhDeltaToKwh(long first, long last, EnphaseQuarterHourAggregate aggregate)
    {
        if (last >= first)
        {
            return (last - first) / 1000m;
        }

        aggregate.IsReliable = false;
        return 0;
    }
}
