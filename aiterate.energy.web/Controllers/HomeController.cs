using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using aiterate.energy.web.Models;
using aiterate.energy.web.Models.Identity;
using aiterate.energy.web.Services;
using aiterate.energy.web.Services.HomeWizard;

namespace aiterate.energy.web.Controllers;

[Authorize]
public class HomeController(
    WebSocketConnectionManager wsManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IHomeWizardTokenProtector homeWizardTokenProtector) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("stream")]
    public async Task Stream()
    {
        var envIp = configuration["HW_IP"] ?? "192.168.1.32";
        var currentUser = await userManager.GetUserAsync(User);
        string? token;
        try
        {
            token = homeWizardTokenProtector.Unprotect(currentUser?.HomeWizardP1Token);
        }
        catch (CryptographicException ex)
        {
            Console.WriteLine($"[Stream] Failed to decrypt HomeWizard P1 token: {ex.Message}");
            await WriteStreamErrorAsync("The stored HomeWizard P1 token can no longer be decrypted. Save the token again in your account settings.");
            return;
        }

        if (currentUser != null
            && !string.IsNullOrWhiteSpace(currentUser.HomeWizardP1Token)
            && !homeWizardTokenProtector.IsProtected(currentUser.HomeWizardP1Token))
        {
            currentUser.HomeWizardP1Token = homeWizardTokenProtector.Protect(currentUser.HomeWizardP1Token);
            await userManager.UpdateAsync(currentUser);
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        await Response.Body.FlushAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            var missingToken = JsonSerializer.Serialize(new
            {
                error = "HomeWizard P1 token is not configured for this user."
            });
            await Response.WriteAsync("data: " + missingToken + "\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        // Determine API host to connect to. Prefer HW_IP if set, otherwise use the API project's default HTTP URL.
        string host;
        if (!string.IsNullOrEmpty(envIp))
        {
            host = envIp; // e.g. 192.168.1.32
        }
        else
        {
            // Default to the API project's HTTP launch URL
            host = "localhost:5172";
        }

        // Determine scheme: allow override via HW_SCHEME (ws or wss) or HW_USE_TLS=1.
        // If not provided, derive from the incoming request (Request.IsHttps).
        var scheme = configuration["HW_SCHEME"];
        if (string.IsNullOrEmpty(scheme))
        {
            if (!string.IsNullOrEmpty(configuration["HW_USE_TLS"]))
            {
                var useTls = configuration["HW_USE_TLS"] == "1";
                scheme = useTls ? "wss" : "ws";
            }
            else
            {
                // Default to request scheme: wss if the request is HTTPS, otherwise ws
                scheme = Request.IsHttps ? "wss" : "ws";
            }
        }

        var unsub = "{\"type\":\"unsubscribe\",\"data\":\"measurement\"}";
        using var ws = new ClientWebSocket();
        var pinnedCertificateSha256 = configuration["HomeWizard:CertificateSha256"];
        if (!string.IsNullOrWhiteSpace(pinnedCertificateSha256))
        {
            ws.Options.RemoteCertificateValidationCallback =
                (_, certificate, _, sslPolicyErrors) => HomeWizardCertificateValidator.IsTrustedCertificate(certificate, sslPolicyErrors, pinnedCertificateSha256);
        }

        // Register connection so it can be closed on application shutdown
        try
        {
            wsManager?.Register(ws, unsub);
        }
        catch
        {
            // ignore registration errors
        }

        var uri = new Uri($"{scheme}://{host}/api/ws");

        // Log and inform client about the backend WebSocket source without exposing token characters.
        const string maskedToken = "********";
        try
        {
            Console.WriteLine($"[Stream] Connecting to {uri} token={maskedToken}");

            var infoJson = $"{{\"source\":\"{scheme}://{host}/api/ws\",\"token_mask\":\"{maskedToken}\"}}";
            var infoMsg = "data: " + infoJson + "\n\n";
            // Send a single informational SSE event to the client
            await Response.WriteAsync(infoMsg);
            await Response.Body.FlushAsync();

            await ws.ConnectAsync(uri, HttpContext.RequestAborted);

            var buffer = new byte[8192];
            var sb = new StringBuilder();

            async Task<string?> ReceiveBackendMessageAsync()
            {
                sb.Clear();
                while (!HttpContext.RequestAborted.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), HttpContext.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                        return null;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    if (result.EndOfMessage)
                    {
                        return sb.ToString();
                    }
                }

                return null;
            }

            async Task ForwardBackendMessageAsync(string msg)
            {
                // Try to parse incoming backend message and deserialize measurement payload server-side.
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(msg);
                    if (doc.RootElement.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "measurement" && doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        var measurement = System.Text.Json.JsonSerializer.Deserialize<HomeWizardMeasurement>(dataEl.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        var labels = new Dictionary<string, string>();
                        foreach (var prop in typeof(HomeWizardMeasurement).GetProperties())
                        {
                            var jsonAttr = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false).FirstOrDefault() as JsonPropertyNameAttribute;
                            var displayAttr = prop.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
                            var key = jsonAttr != null ? jsonAttr.Name : prop.Name;
                            var label = displayAttr?.Name ?? prop.Name;
                            labels[key] = label;
                        }

                        var payload = new { type = "measurement", data = measurement, labels };
                        var ssePayload = "data: " + JsonSerializer.Serialize(payload) + "\n\n";
                        await Response.WriteAsync(ssePayload);
                        await Response.Body.FlushAsync();
                        return;
                    }
                }
                catch
                {
                    // fall back to forwarding raw message
                }

                var sse = "data: " + msg.Replace("\n", "\ndata: ") + "\n\n";
                await Response.WriteAsync(sse);
                await Response.Body.FlushAsync();
            }

            var authorizationRequest = await ReceiveBackendMessageAsync();
            if (authorizationRequest != null)
            {
                await ForwardBackendMessageAsync(authorizationRequest);
            }

            // Authorize
            var auth = $"{{\"type\":\"authorization\",\"data\":\"{token}\"}}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(auth), WebSocketMessageType.Text, true, HttpContext.RequestAborted);

            var authorizationResponse = await ReceiveBackendMessageAsync();
            if (authorizationResponse != null)
            {
                await ForwardBackendMessageAsync(authorizationResponse);
            }

            // Subscribe to measurement
            var sub = "{\"type\":\"subscribe\",\"data\":\"measurement\"}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(sub), WebSocketMessageType.Text, true, HttpContext.RequestAborted);

            while (!HttpContext.RequestAborted.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var msg = await ReceiveBackendMessageAsync();
                if (msg == null)
                {
                    break;
                }

                await ForwardBackendMessageAsync(msg);
            }
        }
        catch (Exception ex)
        {
            var closeStatus = ws.CloseStatus?.ToString();
            var closeDescription = ws.CloseStatusDescription;
            Console.WriteLine($"[Stream] WebSocket error: {ex.Message}; closeStatus={closeStatus}; closeDescription={closeDescription}");
            var err = "data: " + JsonSerializer.Serialize(new
            {
                error = ex.Message,
                close_status = closeStatus,
                close_description = closeDescription
            }) + "\n\n";
            try
            {
                await Response.WriteAsync(err);
                await Response.Body.FlushAsync();
            }
            catch
            {
                // ignore write failures during error handling
            }
        }
        finally
        {
            // Attempt a clean unsubscribe/close so backend stops sending P1 measurement data
            if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
            {
                try
                {
                    // use CancellationToken.None so cleanup runs even if request was aborted
                    await ws.SendAsync(Encoding.UTF8.GetBytes(unsub), WebSocketMessageType.Text, true, CancellationToken.None);
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "unsubscribed", CancellationToken.None);
                }
                catch
                {
                    // ignore cleanup errors
                }
            }

            try
            {
                if (ws is not null)
                {
                    wsManager!.Unregister(ws);
                }
            }
            catch
            {
                // ignore unregister errors
            }
        }
    }

    private async Task WriteStreamErrorAsync(string error)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        await Response.WriteAsync("data: " + JsonSerializer.Serialize(new { error }) + "\n\n");
        await Response.Body.FlushAsync();
    }

    [HttpGet("insights")]
    public IActionResult Insights()
    {
        return View();
    }
}
