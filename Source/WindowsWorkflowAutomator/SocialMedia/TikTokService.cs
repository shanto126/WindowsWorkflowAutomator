using System.Net;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class TikTokService : ITikTokService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public TikTokService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.TikTokAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        return ValidateConnectionAsync(cancellationToken);
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.TikTokAccessTokenProtected = string.Empty;
        _settings.Save();
        _logger.Information("TikTok access token cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("TikTok disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "TikTok integration is not configured.");
        }

        try
        {
            // TikTok's token introspection endpoints vary; do a lightweight probe to the user info endpoint if available.
            // We'll attempt a generic call and map common failures; exact API paths depend on the app setup and API versioning.
            var url = "https://open.tiktokapis.com/v1/user/info/"; // placeholder probe — may require app-level setup
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("TikTok token appears valid.");
            }

            return await MapFailureAsync(response, "TikTok validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "TikTok request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("TikTok validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "TikTok validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> mediaPaths, CancellationToken cancellationToken = default)
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "TikTok integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        // TikTok's Content Posting API has approval/review requirements and may not allow direct publishing for all apps.
        // Here we implement a best-effort approach: if mediaPaths contains a single HTTP/HTTPS URL, attempt a create/publish flow.
        // If local file paths are provided, return Limited with guidance because direct server-side upload may be restricted.

        if (mediaPaths == null || mediaPaths.Count == 0)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "TikTok requires at least one media item.");
        }

        var first = mediaPaths[0];
        if (Uri.TryCreate(first, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            try
            {
                // Placeholder: Many TikTok endpoints require app review and specific parameters. Attempt a generic POST to illustrate flow.
                var url = "https://open.tiktokapis.com/v1/media/create/"; // illustrative; actual endpoint & parameters vary
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var payload = new { media_url = first, caption };
                request.Content = new StringContent(JsonSerializer.Serialize(payload));
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await Http.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return await MapFailureAsync(response, "TikTok publish failed.", cancellationToken);
                }

                var raw = await response.Content.ReadAsStringAsync(cancellationToken);
                var id = TryReadJsonField(raw, "id");
                return PlatformOperationResult.Ok("TikTok post submitted.", id);
            }
            catch (TaskCanceledException)
            {
                return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "TikTok request timed out.");
            }
            catch (Exception ex)
            {
                _logger.Error("TikTok publish failed.", ex);
                return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "TikTok publish failed.");
            }
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Local file uploads to TikTok are not supported by this adapter. Use a publicly accessible media URL or configure server-side uploads via TikTok APIs that require app approval.");
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string mediaPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Media path is empty."));
        }

        if (Uri.TryCreate(mediaPath, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return Task.FromResult(PlatformOperationResult.Ok("Media is a public URL.", externalId: mediaPath));
        }

        return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Local file uploads to TikTok are not supported by this adapter."));
    }

    private string ReadToken()
    {
        try
        {
            return string.IsNullOrWhiteSpace(_settings.Current.TikTokAccessTokenProtected)
                ? string.Empty
                : _protector.Unprotect(_settings.Current.TikTokAccessTokenProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read TikTok token from secure settings.", ex);
            return string.Empty;
        }
    }

    private async Task<PlatformOperationResult> MapFailureAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "TikTok token is invalid or expired.");
        }

        if ((int)response.StatusCode == 429 || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "TikTok API rate limit reached.");
        }

        if ((int)response.StatusCode >= 500)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "TikTok service is currently unavailable.");
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Error, fallbackMessage + " " + body);
    }

    private static string? TryReadJsonField(string rawJson, string field)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
}