using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace aiterate.energy.web.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(basePath, "appsettings.json"))
            && Directory.Exists(Path.Combine(basePath, "aiterate.energy.web")))
        {
            basePath = Path.Combine(basePath, "aiterate.energy.web");
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Migrations")
            ?? configuration.GetConnectionString("Identity")
            ?? throw new InvalidOperationException("Connection string 'Identity' was not found.");
        var serverVersion = configuration["MariaDb:ServerVersion"] ?? "10.11.0";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(
                connectionString,
                new MariaDbServerVersion(Version.Parse(serverVersion)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure())
            .Options;

        return new ApplicationDbContext(options);
    }
}
