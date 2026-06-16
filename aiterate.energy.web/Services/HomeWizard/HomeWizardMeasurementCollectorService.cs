using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using aiterate.energy.web.Data;
using aiterate.energy.web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace aiterate.energy.web.Services.HomeWizard;

public class HomeWizardMeasurementCollectorService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IHomeWizardTokenProtector tokenProtector,
    IOptions<HomeWizardCollectorOptions> options,
    TimeProvider timeProvider,
    ILogger<HomeWizardMeasurementCollectorService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HomeWizardCollectorOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("HomeWizard collector is disabled.");
            return;
        }

        if (_options.BucketMinutes <= 0 || 60 % _options.BucketMinutes != 0)
        {
            logger.LogError("HomeWizard collector bucket size must divide one hour. Configured value: {BucketMinutes}", _options.BucketMinutes);
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        logger.LogInformation(
            "HomeWizard collector started. Endpoint={Scheme}://{Host}/api/measurement, interval={IntervalSeconds}s, bucket={BucketMinutes}m.",
            _options.Scheme,
            _options.Host,
            interval.TotalSeconds,
            _options.BucketMinutes);

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
            var token = await ResolveTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("HomeWizard collector is enabled, but no token is configured or stored.");
                return;
            }

            var measurement = await FetchMeasurementAsync(token, cancellationToken);
            if (measurement is null)
            {
                return;
            }

            var measuredAt = HomeWizardTimestampParser.ParseLocalOrNow(measurement.Timestamp, timeProvider);
            var periodStart = RoundDown(measuredAt, _options.BucketMinutes);

            await UpsertAggregateAsync(periodStart, measuredAt, measurement, cancellationToken);
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(ex, "HomeWizard collector cannot decrypt the stored P1 token. Save the token again for the development database.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HomeWizard collector poll failed.");
        }
    }

    private async Task<HomeWizardMeasurement?> FetchMeasurementAsync(string token, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(nameof(HomeWizardMeasurementCollectorService));
        client.BaseAddress = new Uri($"{_options.Scheme}://{_options.Host}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Version", "2");

        using var response = await client.GetAsync("/api/measurement", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "HomeWizard measurement request to {Endpoint} failed with HTTP {StatusCode}.",
                client.BaseAddress,
                (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<HomeWizardMeasurement>(stream, JsonOptions, cancellationToken);
    }

    private async Task<string?> ResolveTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            return _options.Token;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedTokens = await dbContext.Users
            .Where(user => user.HomeWizardP1Token != null && user.HomeWizardP1Token != "")
            .Select(user => user.HomeWizardP1Token)
            .ToListAsync(cancellationToken);

        foreach (var storedToken in storedTokens)
        {
            if (string.IsNullOrWhiteSpace(storedToken))
            {
                continue;
            }

            if (!tokenProtector.IsProtected(storedToken))
            {
                return storedToken;
            }

            try
            {
                return tokenProtector.Unprotect(storedToken);
            }
            catch (CryptographicException ex)
            {
                logger.LogWarning(ex, "Skipping stored HomeWizard P1 token because it cannot be decrypted by this app instance.");
            }
        }

        return null;
    }

    private async Task UpsertAggregateAsync(
        DateTime periodStart,
        DateTime measuredAt,
        HomeWizardMeasurement measurement,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aggregate = await dbContext.HomeWizardQuarterHourAggregates
            .SingleOrDefaultAsync(x => x.PeriodStart == periodStart, cancellationToken);

        if (aggregate is null)
        {
            aggregate = HomeWizardAggregateUpdater.Create(periodStart, _options.BucketMinutes, measuredAt, measurement);
            dbContext.HomeWizardQuarterHourAggregates.Add(aggregate);
        }
        else if (measuredAt > aggregate.LastSeenAt)
        {
            HomeWizardAggregateUpdater.Update(aggregate, measuredAt, measurement);
        }
        else
        {
            logger.LogDebug(
                "Skipping HomeWizard sample at {MeasuredAt}; aggregate {PeriodStart} already has {LastSeenAt}.",
                measuredAt,
                periodStart,
                aggregate.LastSeenAt);
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "HomeWizard aggregate saved. PeriodStart={PeriodStart}, SampleCount={SampleCount}, ImportKwh={ImportKwh}, ExportKwh={ExportKwh}.",
            aggregate.PeriodStart,
            aggregate.SampleCount,
            aggregate.EnergyImportKwh,
            aggregate.EnergyExportKwh);
    }

    private static DateTime RoundDown(DateTime value, int bucketMinutes)
    {
        var minute = value.Minute / bucketMinutes * bucketMinutes;
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, minute, 0, value.Kind);
    }
}
