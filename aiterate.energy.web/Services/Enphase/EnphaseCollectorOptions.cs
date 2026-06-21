namespace aiterate.energy.web.Services.Enphase;

public class EnphaseCollectorOptions
{
    public bool Enabled { get; set; }

    public string Scheme { get; set; } = "https";

    public string Host { get; set; } = "";

    public string Endpoint { get; set; } = "/production.json";

    public string? Token { get; set; }

    public int PollIntervalSeconds { get; set; } = 300;

    public int BucketMinutes { get; set; } = 15;

    public string TimeZoneId { get; set; } = "Europe/Amsterdam";

    public bool AllowInvalidCertificate { get; set; } = true;
}
