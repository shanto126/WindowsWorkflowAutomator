using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class YouTubeService : IYouTubeService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public YouTubeService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.YouTubeAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        return ValidateConnectionAsync(cancellationToken);
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.YouTubeAccessTokenProtected = string.Empty;
        _settings.Save();
        _logger.Information("YouTube access token cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("YouTube disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "YouTube integration is not configured.");
        }

        try
        {
            // Use tokeninfo endpoint to validate access token
            var url = $"https://oauth2.googleapis.com/tokeninfo?access_token={Uri.EscapeDataString(token)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("YouTube token appears valid.");
            }

            return await MapFailureAsync(response, "YouTube token validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "YouTube request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("YouTube validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "YouTube validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(string title, IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "YouTube integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (files == null || files.Count == 0)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "YouTube requires a video file to upload.");
        }

        var file = files[0];
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Video file not found.");
        }

        try
        {
            // Prepare metadata
            var snippet = new
            {
                title = title ?? string.Empty,
                description = string.Empty
            };

            var statusObj = new { privacyStatus = "public" };

            var metadata = new { snippet, status = statusObj };
            var metadataJson = JsonSerializer.Serialize(metadata);

            // Build multipart/related body for simple upload (small videos). Use uploadType=multipart
            var uploadUrl = "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=multipart&part=snippet,status";
            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var boundary = "===============YouTubeBoundary==" + Guid.NewGuid().ToString("N");
            var content = new MultipartContent("related", boundary);

            var jsonContent = new StringContent(metadataJson);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(jsonContent);

            var mediaBytes = await File.ReadAllBytesAsync(file, cancellationToken);
            var mediaContent = new ByteArrayContent(mediaBytes);
            mediaContent.Headers.ContentType = new MediaTypeHeaderValue("video/*");
            content.Add(mediaContent);

            request.Content = content;

            var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailureAsync(response, "YouTube upload failed.", cancellationToken);
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var id = TryReadJsonField(raw, "id");
            return PlatformOperationResult.Ok("YouTube video uploaded.", id);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "YouTube upload timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("YouTube upload failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "YouTube upload failed.");
        }
    }

    public async Task<PlatformOperationResult> UploadMediaAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // For YouTube, treat UploadMedia as a wrapper around CreatePostAsync that uploads the file
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Video file not found.");
        }

        // Do not publish metadata here; return ExternalId after a minimal upload attempt is done in CreatePostAsync.
        return PlatformOperationResult.Ok("File ready for upload.", externalId: filePath);
    }

    private string ReadToken()
    {
        try
        {
            return string.IsNullOrWhiteSpace(_settings.Current.YouTubeAccessTokenProtected)
                ? string.Empty
                : _protector.Unprotect(_settings.Current.YouTubeAccessTokenProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read YouTube token from secure settings.", ex);
            return string.Empty;
        }
    }

    private async Task<PlatformOperationResult> MapFailureAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "YouTube token is invalid or expired.");
        }

        if ((int)response.StatusCode == 429 || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "YouTube API rate limit reached.");
        }

        if ((int)response.StatusCode >= 500)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "YouTube service is currently unavailable.");
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
