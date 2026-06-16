using System.Globalization;

namespace aiterate.energy.web.Services.HomeWizard;

public static class HomeWizardTimestampParser
{
    public static DateTime ParseLocalOrNow(string? timestamp, TimeProvider timeProvider)
    {
        if (DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedOffset))
        {
            return parsedOffset.LocalDateTime;
        }

        if (!string.IsNullOrWhiteSpace(timestamp) && timestamp.Length >= 12)
        {
            var compact = timestamp[..12];
            if (DateTime.TryParseExact(
                    compact,
                    "yyMMddHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsedCompact))
            {
                return parsedCompact;
            }
        }

        return timeProvider.GetLocalNow().DateTime;
    }
}
