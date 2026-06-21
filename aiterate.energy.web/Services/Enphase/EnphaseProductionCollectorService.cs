using System.Net.Http.Headers;
using System.Text.Json;
using aiterate.energy.web.Data;
using aiterate.energy.web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace aiterate.energy.web.Services.Enphase;

public class EnphaseProductionCollectorService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<EnphaseCollectorOptions> options,
    TimeProvider timeProvider,
    ILogger<EnphaseProductionCollectorService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EnphaseCollectorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Enphase collector is disabled.");
            return;
        }

        if (_options.BucketMinutes <= 0 || 60 % _options.BucketMinutes != 0)
        {
            logger.LogError("Enphase collector bucket size must divide one hour. Configured value: {BucketMinutes}", _options.BucketMinutes);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            logger.LogError("Enphase collector is enabled, but EnphaseCollector:Host is not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            logger.LogError("Enphase collector is enabled, but EnphaseCollector:Token is not configured.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        logger.LogInformation(
            "Enphase collector started. Endpoint={Scheme}://{Host}{Endpoint}, interval={IntervalSeconds}s, bucket={BucketMinutes}m, timeZone={TimeZoneId}.",
            _options.Scheme,
            _options.Host,
            _options.Endpoint,
            interval.TotalSeconds,
            _options.BucketMinutes,
            _options.TimeZoneId);

        await PollOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reading = await FetchProductionReadingAsync(cancellationToken);
            if (reading is null)
            {
                return;
            }

            var measuredAt = ParseReadingTimeOrNow(reading.ReadingTime);
            var periodStart = RoundDown(measuredAt, _options.BucketMinutes);

            await UpsertAggregateAsync(periodStart, measuredAt, reading, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Enphase collector poll failed.");
        }
    }

    private async Task<EnphaseProductionReading?> FetchProductionReadingAsync(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(EnphaseProductionCollectorService));
        client.BaseAddress = new Uri($"{_options.Scheme}://{_options.Host}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);

        using var response = await client.GetAsync(_options.Endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Enphase production request to {Endpoint} failed with HTTP {StatusCode}.",
                client.BaseAddress,
                (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var productionResponse = await JsonSerializer.DeserializeAsync<EnphaseProductionResponse>(stream, JsonOptions, cancellationToken);
        var reading = productionResponse?.Production
            .FirstOrDefault(item => string.Equals(item.Type, "inverters", StringComparison.OrdinalIgnoreCase));

        if (reading is null)
        {
            logger.LogWarning("Enphase production response did not include an inverter production reading.");
        }

        return reading;
    }

    private async Task UpsertAggregateAsync(
        DateTime periodStart,
        DateTime measuredAt,
        EnphaseProductionReading reading,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aggregate = await dbContext.EnphaseQuarterHourAggregates
            .SingleOrDefaultAsync(x => x.PeriodStart == periodStart, cancellationToken);

        if (aggregate is null)
        {
            aggregate = EnphaseAggregateUpdater.Create(periodStart, _options.BucketMinutes, measuredAt, reading);
            dbContext.EnphaseQuarterHourAggregates.Add(aggregate);
        }
        else if (measuredAt > aggregate.LastSeenAt)
        {
            EnphaseAggregateUpdater.Update(aggregate, measuredAt, reading);
        }
        else
        {
            logger.LogDebug(
                "Skipping Enphase sample at {MeasuredAt}; aggregate {PeriodStart} already has {LastSeenAt}.",
                measuredAt,
                periodStart,
                aggregate.LastSeenAt);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Enphase aggregate saved. PeriodStart={PeriodStart}, SampleCount={SampleCount}, ProductionKwh={ProductionKwh}, AveragePowerW={AveragePowerW}.",
            aggregate.PeriodStart,
            aggregate.SampleCount,
            aggregate.EnergyProductionKwh,
            aggregate.AveragePowerW);
    }

    private DateTime ParseReadingTimeOrNow(long readingTime)
    {
        var timeZone = ResolveTimeZone();
        if (readingTime <= 0)
        {
            return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
        }

        return TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(readingTime), timeZone).DateTime;
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        if (!string.IsNullOrWhiteSpace(_options.TimeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                logger.LogWarning(ex, "Configured EnphaseCollector:TimeZoneId '{TimeZoneId}' was not found. Falling back to local timezone.", _options.TimeZoneId);
            }
            catch (InvalidTimeZoneException ex)
            {
                logger.LogWarning(ex, "Configured EnphaseCollector:TimeZoneId '{TimeZoneId}' is invalid. Falling back to local timezone.", _options.TimeZoneId);
            }
        }

        return TimeZoneInfo.Local;
    }

    private static DateTime RoundDown(DateTime value, int bucketMinutes)
    {
        var minute = value.Minute / bucketMinutes * bucketMinutes;
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, minute, 0, value.Kind);
    }
}
