using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class FacebookService : IFacebookService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public FacebookService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public async Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.FacebookAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (validation.Succeeded)
        {
            _logger.Information("Facebook connection validated.");
        }

        return validation;
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.FacebookAccessTokenProtected = string.Empty;
        _settings.Save();
        _logger.Information("Facebook access token cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("Facebook disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.NotConfigured,
                "Facebook integration is not configured.");
        }

        try
        {
            var url = $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.PageId)}" +
                      $"?fields=id,name&access_token={Uri.EscapeDataString(config.AccessToken)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("Facebook connection is valid.");
            }

            return await MapFailureAsync(response, "Facebook connection validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Facebook request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Facebook validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Facebook validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(
        string caption,
        IReadOnlyList<string> images,
        CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.NotConfigured,
                "Facebook integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var mediaIds = new List<string>();
        foreach (var image in images)
        {
            var upload = await UploadMediaAsync(image, cancellationToken);
            if (!upload.Succeeded)
            {
                return upload;
            }

            if (!string.IsNullOrWhiteSpace(upload.ExternalId))
            {
                mediaIds.Add(upload.ExternalId);
            }
        }

        var payload = new Dictionary<string, string>
        {
            ["message"] = caption,
            ["access_token"] = config.AccessToken
        };

        for (var i = 0; i < mediaIds.Count; i++)
        {
            payload[$"attached_media[{i}]"] = $"{{\"media_fbid\":\"{mediaIds[i]}\"}}";
        }

        using var content = new FormUrlEncodedContent(payload);
        using var response = await Http.PostAsync(
            $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.PageId)}/feed",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return await MapFailureAsync(response, "Facebook post creation failed.", cancellationToken);
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var id = TryReadJsonField(raw, "id");
        return PlatformOperationResult.Ok("Facebook post published.", id);
    }

    public async Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.NotConfigured,
                "Facebook integration is not configured.");
        }

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Image file not found.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.PageId)}/photos");

            using var multipart = new MultipartFormDataContent();
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(imageContent, "source", Path.GetFileName(imagePath));
            multipart.Add(new StringContent("false"), "published");
            multipart.Add(new StringContent(config.AccessToken), "access_token");
            request.Content = multipart;

            var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailureAsync(response, "Facebook media upload failed.", cancellationToken);
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var id = TryReadJsonField(raw, "id");
            return PlatformOperationResult.Ok("Media uploaded.", id);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Facebook upload timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Facebook media upload failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Facebook media upload failed.");
        }
    }

    private async Task<PlatformOperationResult> MapFailureAsync(
        HttpResponseMessage response,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.AuthFailure,
                "Facebook token is invalid or expired.");
        }

        if ((int)response.StatusCode == 429 || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "Facebook API rate limit reached.");
        }

        if (lower.Contains("permissions") || lower.Contains("permission"))
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.InvalidPermissions,
                "Facebook page permissions are missing or invalid.");
        }

        if ((int)response.StatusCode >= 500)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Facebook service is currently unavailable.");
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Error, fallbackMessage);
    }

    private FacebookConfig ReadConfig()
    {
        var appId = _settings.Current.FacebookAppId?.Trim() ?? string.Empty;
        var pageId = _settings.Current.FacebookPageId?.Trim() ?? string.Empty;
        var token = string.Empty;

        try
        {
            token = string.IsNullOrWhiteSpace(_settings.Current.FacebookAccessTokenProtected)
                ? string.Empty
                : _protector.Unprotect(_settings.Current.FacebookAccessTokenProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read Facebook token from secure settings.", ex);
        }

        return new FacebookConfig(appId, pageId, token);
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

    private readonly record struct FacebookConfig(string AppId, string PageId, string AccessToken)
    {
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AppId)
            && !string.IsNullOrWhiteSpace(PageId)
            && !string.IsNullOrWhiteSpace(AccessToken);
    }
}
