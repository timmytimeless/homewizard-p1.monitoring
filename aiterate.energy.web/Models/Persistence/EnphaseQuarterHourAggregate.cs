using System.ComponentModel.DataAnnotations;

namespace aiterate.energy.web.Models.Persistence;

public class EnphaseQuarterHourAggregate
{
    [Key]
    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public int SampleCount { get; set; }

    public bool IsReliable { get; set; } = true;

    public int ActiveInverterCount { get; set; }

    public long FirstLifetimeProductionWh { get; set; }

    public long LastLifetimeProductionWh { get; set; }

    public decimal EnergyProductionKwh { get; set; }

    public decimal AveragePowerW { get; set; }

    public int MinimumPowerW { get; set; }

    public int MaximumPowerW { get; set; }
}
