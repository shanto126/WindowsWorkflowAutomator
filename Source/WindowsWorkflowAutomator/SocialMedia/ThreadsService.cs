using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class ThreadsService : IThreadsService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public ThreadsService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.ThreadsAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        return ValidateConnectionAsync(cancellationToken);
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.ThreadsAccessTokenProtected = string.Empty;
        _settings.Current.ThreadsUserId = string.Empty;
        _settings.Save();
        _logger.Information("Threads access token and user id cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("Threads disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Threads integration is not configured.");
        }

        try
        {
            var url = $"https://graph.threads.net/v1.0/{Uri.EscapeDataString(config.UserId)}?fields=id,username&access_token={Uri.EscapeDataString(config.AccessToken)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("Threads connection is valid.");
            }

            return await MapFailureAsync(response, "Threads connection validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Threads request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Threads validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Threads validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> media, CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Threads integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var entries = media?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList() ?? [];

        if (entries.Count == 0)
        {
            return await PublishTextOnlyPostAsync(config, caption, cancellationToken);
        }

        if (entries.Count > 1)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Threads only supports a single media item per post in this implementation.");
        }

        var mediaUrl = entries[0];
        if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Threads publishing requires a public HTTP/HTTPS media URL. Local file uploads are not supported by this adapter.");
        }

        var isVideo = IsVideoUrl(mediaUrl);
        try
        {
            var createUrl = $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.UserId)}/threads";
            var payload = new Dictionary<string, string>
            {
                ["access_token"] = config.AccessToken,
                ["text"] = caption ?? string.Empty,
                ["media_type"] = isVideo ? "VIDEO" : "IMAGE",
                [isVideo ? "video_url" : "image_url"] = mediaUrl
            };

            using var createRequest = new HttpRequestMessage(HttpMethod.Post, createUrl);
            createRequest.Content = new FormUrlEncodedContent(payload);
            using var createResponse = await Http.SendAsync(createRequest, cancellationToken);
            if (!createResponse.IsSuccessStatusCode)
            {
                return await MapFailureAsync(createResponse, "Threads media creation failed.", cancellationToken);
            }

            var createRaw = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            var creationId = TryReadJsonField(createRaw, "id");
            if (string.IsNullOrWhiteSpace(creationId))
            {
                return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Threads media creation did not return a creation id.");
            }

            var publishUrl = $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.UserId)}/threads_publish";
            using var publishRequest = new HttpRequestMessage(HttpMethod.Post, publishUrl);
            publishRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["access_token"] = config.AccessToken,
                ["creation_id"] = creationId
            });
            using var publishResponse = await Http.SendAsync(publishRequest, cancellationToken);
            if (!publishResponse.IsSuccessStatusCode)
            {
                return await MapFailureAsync(publishResponse, "Threads publish failed.", cancellationToken);
            }

            var publishRaw = await publishResponse.Content.ReadAsStringAsync(cancellationToken);
            var mediaId = TryReadJsonField(publishRaw, "id");
            return PlatformOperationResult.Ok(isVideo ? "Threads video published." : "Threads post published.", mediaId);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Threads request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Threads publish failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Threads publish failed.");
        }
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string mediaPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Media path is empty."));
        }

        if (Uri.TryCreate(mediaPath, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return Task.FromResult(PlatformOperationResult.Ok("Media URL is ready for Threads publishing.", externalId: mediaPath));
        }

        return Task.FromResult(PlatformOperationResult.Fail(
            PlatformOperationStatus.Limited,
            "Threads publishing requires a public HTTP/HTTPS media URL. Local file uploads are not supported by this adapter."));
    }

    private async Task<PlatformOperationResult> PublishTextOnlyPostAsync(ThreadsConfig config, string? caption, CancellationToken cancellationToken)
    {
        try
        {
            var publishUrl = $"https://graph.facebook.com/v20.0/{Uri.EscapeDataString(config.UserId)}/threads_publish";
            using var request = new HttpRequestMessage(HttpMethod.Post, publishUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["access_token"] = config.AccessToken,
                ["text"] = caption ?? string.Empty
            });

            using var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailureAsync(response, "Threads text post failed.", cancellationToken);
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var id = TryReadJsonField(raw, "id");
            return PlatformOperationResult.Ok("Threads text post published.", id);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Threads request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Threads text post failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Threads text post failed.");
        }
    }

    private ThreadsConfig ReadConfig()
    {
        var token = string.IsNullOrWhiteSpace(_settings.Current.ThreadsAccessTokenProtected)
            ? string.Empty
            : _protector.Unprotect(_settings.Current.ThreadsAccessTokenProtected);
        var userId = _settings.Current.ThreadsUserId?.Trim() ?? string.Empty;
        return new ThreadsConfig(userId, token);
    }

    private async Task<PlatformOperationResult> MapFailureAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "Threads token is invalid or expired.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "Threads rate limit reached. Please retry later.");
        }

        if (lower.Contains("not found") || lower.Contains("invalid") || lower.Contains("permission"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.InvalidPermissions, "Threads permissions, user id, or URL are invalid.");
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Error, string.IsNullOrWhiteSpace(body) ? fallbackMessage : body);
    }

    private static bool IsVideoUrl(string mediaUrl)
    {
        var ext = Path.GetExtension(mediaUrl).ToLowerInvariant();
        return ext is ".mp4" or ".mov" or ".avi" or ".m4v" or ".webm";
    }

    private static string TryReadJsonField(string raw, string fieldName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.TryGetProperty(fieldName, out var node))
            {
                return node.GetString() ?? string.Empty;
            }

            if (document.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty(fieldName, out var dataNode))
            {
                return dataNode.GetString() ?? string.Empty;
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private readonly record struct ThreadsConfig(string UserId, string AccessToken)
    {
        public bool IsConfigured => !string.IsNullOrWhiteSpace(UserId) && !string.IsNullOrWhiteSpace(AccessToken);
    }
}
