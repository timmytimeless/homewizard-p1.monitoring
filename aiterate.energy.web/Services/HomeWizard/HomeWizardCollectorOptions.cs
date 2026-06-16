namespace aiterate.energy.web.Services.HomeWizard;

public class HomeWizardCollectorOptions
{
    public bool Enabled { get; set; }

    public string Scheme { get; set; } = "https";

    public string Host { get; set; } = "192.168.1.32";

    public string? Token { get; set; }

    public int PollIntervalSeconds { get; set; } = 60;

    public int BucketMinutes { get; set; } = 15;
}
