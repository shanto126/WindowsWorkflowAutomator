using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class InstagramService : IInstagramService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public InstagramService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.InstagramAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        return ValidateConnectionAsync(cancellationToken);
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.InstagramAccessTokenProtected = string.Empty;
        _settings.Save();
        _logger.Information("Instagram access token cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("Instagram disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Instagram integration is not configured.");
        }

        try
        {
            // Query the Instagram user to validate the token and user id.
            var url = $"https://graph.facebook.com/v17.0/{Uri.EscapeDataString(config.InstagramUserId)}?fields=id,username&access_token={Uri.EscapeDataString(config.AccessToken)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("Instagram connection is valid.");
            }

            return await MapFailureAsync(response, "Instagram connection validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Instagram request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Instagram validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Instagram validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(string caption, IReadOnlyList<string> images, CancellationToken cancellationToken = default)
    {
        var config = ReadConfig();
        if (!config.IsConfigured)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Instagram integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (images == null || images.Count == 0)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Instagram requires at least one image URL.");
        }

        // Instagram Graph API supports single-image posts via creating a media container using a public image URL.
        // Local file uploads are NOT supported here because Instagram requires a publicly accessible image URL for /{ig-user-id}/media.
        // For simplicity: support only a single image that is an HTTP/HTTPS URL. Multiple images (carousel) are not implemented here.

        if (images.Count > 1)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Multiple-image (carousel) posts are not implemented. Only a single public image URL is supported.");
        }

        var image = images[0];
        if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Instagram publishing requires a publicly accessible HTTP/HTTPS image URL. Local file uploads are not supported by this adapter.");
        }

        try
        {
            // 1) Create media container
            var createUrl = $"https://graph.facebook.com/v17.0/{Uri.EscapeDataString(config.InstagramUserId)}/media";
            var payload = new Dictionary<string, string>
            {
                ["image_url"] = image,
                ["caption"] = caption ?? string.Empty,
                ["access_token"] = config.AccessToken
            };

            using var createContent = new FormUrlEncodedContent(payload);
            using var createResp = await Http.PostAsync(createUrl, createContent, cancellationToken);
            if (!createResp.IsSuccessStatusCode)
            {
                return await MapFailureAsync(createResp, "Instagram media creation failed.", cancellationToken);
            }

            var createRaw = await createResp.Content.ReadAsStringAsync(cancellationToken);
            var creationId = TryReadJsonField(createRaw, "id");
            if (string.IsNullOrWhiteSpace(creationId))
            {
                return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Instagram media creation did not return a creation id.");
            }

            // 2) Publish the media
            var publishUrl = $"https://graph.facebook.com/v17.0/{Uri.EscapeDataString(config.InstagramUserId)}/media_publish";
            var publishPayload = new Dictionary<string, string>
            {
                ["creation_id"] = creationId,
                ["access_token"] = config.AccessToken
            };

            using var publishContent = new FormUrlEncodedContent(publishPayload);
            using var publishResp = await Http.PostAsync(publishUrl, publishContent, cancellationToken);
            if (!publishResp.IsSuccessStatusCode)
            {
                return await MapFailureAsync(publishResp, "Instagram publish failed.", cancellationToken);
            }

            var publishRaw = await publishResp.Content.ReadAsStringAsync(cancellationToken);
            var mediaId = TryReadJsonField(publishRaw, "id");
            return PlatformOperationResult.Ok("Instagram post published.", mediaId);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Instagram request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Instagram publish failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Instagram publish failed.");
        }
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        // Instagram Graph API requires publicly accessible image URLs for creating media containers.
        // If caller supplies a public URL, return it as ExternalId so caller can pass it to CreatePostAsync.
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Image path is empty."));
        }

        if (Uri.TryCreate(imagePath, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            // Return the URL as the ExternalId so CreatePostAsync can use it.
            return Task.FromResult(PlatformOperationResult.Ok("Image is a public URL.", externalId: imagePath));
        }

        return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Limited, "Local file uploads are not supported for Instagram. Provide a publicly accessible image URL."));
    }

    private async Task<PlatformOperationResult> MapFailureAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "Instagram token is invalid or expired.");
        }

        if ((int)response.StatusCode == 429 || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "Instagram API rate limit reached.");
        }

        if (lower.Contains("permissions") || lower.Contains("permission"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.InvalidPermissions, "Instagram permissions are missing or invalid.");
        }

        if ((int)response.StatusCode >= 500)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Instagram service is currently unavailable.");
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Error, fallbackMessage + " " + body);
    }

    private InstagramConfig ReadConfig()
    {
        var token = string.Empty;
        try
        {
            token = string.IsNullOrWhiteSpace(_settings.Current.InstagramAccessTokenProtected)
                ? string.Empty
                : _protector.Unprotect(_settings.Current.InstagramAccessTokenProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read Instagram token from secure settings.", ex);
        }

        var igUserId = _settings.Current.InstagramUserId?.Trim() ?? string.Empty;
        return new InstagramConfig(igUserId, token);
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

    private readonly record struct InstagramConfig(string InstagramUserId, string AccessToken)
    {
        public bool IsConfigured => !string.IsNullOrWhiteSpace(InstagramUserId) && !string.IsNullOrWhiteSpace(AccessToken);
    }
}
