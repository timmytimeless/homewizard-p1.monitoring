using System.Text.Json.Serialization;

namespace aiterate.energy.web.Models;

public class EnphaseProductionResponse
{
    [JsonPropertyName("production")]
    public List<EnphaseProductionReading> Production { get; set; } = [];

    [JsonPropertyName("storage")]
    public List<EnphaseStorageReading> Storage { get; set; } = [];
}

public class EnphaseProductionReading
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("activeCount")]
    public int ActiveCount { get; set; }

    [JsonPropertyName("readingTime")]
    public long ReadingTime { get; set; }

    [JsonPropertyName("wNow")]
    public int PowerW { get; set; }

    [JsonPropertyName("whLifetime")]
    public long LifetimeProductionWh { get; set; }
}

public class EnphaseStorageReading
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("activeCount")]
    public int ActiveCount { get; set; }

    [JsonPropertyName("readingTime")]
    public long ReadingTime { get; set; }

    [JsonPropertyName("wNow")]
    public int PowerW { get; set; }

    [JsonPropertyName("whNow")]
    public int EnergyWh { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}
