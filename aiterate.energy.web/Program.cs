using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using aiterate.energy.web.Data;
using aiterate.energy.web.Models.Identity;
using aiterate.energy.web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Try to free configured dev ports (so restarting from Rider doesn't fail if previous run didn't stop cleanly)
try
{
    void KillProcessesOnPorts(string urlsCsv)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (string.IsNullOrWhiteSpace(urlsCsv)) return;

        var user = Environment.UserName;
        var parts = urlsCsv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            try
            {
                var uri = new Uri(p);
                var port = uri.Port;
                // Limit to dotnet processes owned by current user to avoid killing unrelated services
                var psi = new ProcessStartInfo("lsof", $"-tiTCP:{port} -sTCP:LISTEN -a -u {user} -c dotnet")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) continue;
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(1000);
                foreach (var line in output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!int.TryParse(line.Trim(), out var pid)) continue;
                    
                    try
                    {
                        if (pid == Process.GetCurrentProcess().Id) continue;

                        // Try graceful termination first
                        Console.WriteLine($"Attempting SIGTERM on PID {pid} listening on port {port}");
                        var term = new ProcessStartInfo("kill", $"-15 {pid}") { UseShellExecute = false };
                        Process.Start(term)?.WaitForExit(500);

                        // Wait briefly and check
                        try
                        {
                            var pinfo = Process.GetProcessById(pid);
                            if (!pinfo.HasExited)
                            {
                                Console.WriteLine($"PID {pid} did not exit; forcing SIGKILL");
                                var kill = new ProcessStartInfo("kill", $"-9 {pid}") { UseShellExecute = false };
                                Process.Start(kill)?.WaitForExit(500);
                            }
                        }
                        catch (ArgumentException) { /* process already exited */ }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to terminate pid {line.Trim()}: {ex.Message}");
                    }
                }
            }
            catch
            {
                // ignore bad URIs or lsof failures
            }
        }
    }

    var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5095;https://localhost:7112";
    KillProcessesOnPorts(urlsEnv);
}
catch (Exception ex)
{
    Console.WriteLine($"Port-kill helper failed: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("aiterate.energy.web");

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// Add services to the container.
var identityConnectionString = builder.Configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException("Connection string 'Identity' was not found.");
var mariaDbServerVersion = builder.Configuration["MariaDb:ServerVersion"] ?? "10.11.0";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        identityConnectionString,
        new MariaDbServerVersion(Version.Parse(mariaDbServerVersion)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IHomeWizardTokenProtector, HomeWizardTokenProtector>();
// Register WebSocket connection manager to ensure active backend WS connections can be closed on shutdown
builder.Services.AddSingleton<aiterate.energy.web.Services.WebSocketConnectionManager>();

var app = builder.Build();

// Ensure open backend WebSocket connections are closed and unsubscribed on application shutdown
var lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        var mgr = app.Services.GetRequiredService<aiterate.energy.web.Services.WebSocketConnectionManager>();
        mgr.CloseAllAsync().GetAwaiter().GetResult();
    }
    catch
    {
        // ignore shutdown errors
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
