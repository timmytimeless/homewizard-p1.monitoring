using aiterate.energy.web.Data;
using aiterate.energy.web.Services;
using aiterate.energy.web.Services.HomeWizard;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("aiterate.energy.web");

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    if (!Path.IsPathRooted(dataProtectionKeysPath))
    {
        dataProtectionKeysPath = Path.GetFullPath(dataProtectionKeysPath, builder.Environment.ContentRootPath);
    }

    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

var identityConnectionString = builder.Configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException("Connection string 'Identity' was not found.");
var mariaDbServerVersion = builder.Configuration["MariaDb:ServerVersion"] ?? "10.11.0";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        identityConnectionString,
        new MariaDbServerVersion(Version.Parse(mariaDbServerVersion)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services.AddSingleton<IHomeWizardTokenProtector, HomeWizardTokenProtector>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<HomeWizardCollectorOptions>(builder.Configuration.GetSection("HomeWizardCollector"));
builder.Services.AddHttpClient(nameof(HomeWizardMeasurementCollectorService))
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var pinnedCertificateSha256 = config["HomeWizard:CertificateSha256"];

        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, sslPolicyErrors) =>
                HomeWizardCertificateValidator.IsTrustedCertificate(certificate, sslPolicyErrors, pinnedCertificateSha256)
        };
    });
builder.Services.AddHostedService<HomeWizardMeasurementCollectorService>();

await builder.Build().RunAsync();
