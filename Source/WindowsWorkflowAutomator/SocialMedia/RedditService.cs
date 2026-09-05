using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Security;

namespace WindowsWorkflowAutomator.SocialMedia;

public sealed class RedditService : IRedditService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly IAppSettingsService _settings;
    private readonly ISecretProtector _protector;
    private readonly IAppLogger _logger;

    public RedditService(IAppSettingsService settings, ISecretProtector protector, IAppLogger logger)
    {
        _settings = settings;
        _protector = protector;
        _logger = logger;
    }

    public Task<PlatformOperationResult> ConnectAsync(string? accessToken = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _settings.Current.RedditAccessTokenProtected = _protector.Protect(accessToken.Trim());
            _settings.Save();
        }

        return ValidateConnectionAsync(cancellationToken);
    }

    public Task<PlatformOperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _settings.Current.RedditAccessTokenProtected = string.Empty;
        _settings.Save();
        _logger.Information("Reddit access token cleared.");
        return Task.FromResult(PlatformOperationResult.Ok("Reddit disconnected."));
    }

    public async Task<PlatformOperationResult> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Reddit integration is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://oauth.reddit.com/api/v1/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await Http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PlatformOperationResult.Ok("Reddit token appears valid.");
            }

            return await MapFailureAsync(response, "Reddit token validation failed.", cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Reddit request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Reddit validation failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Reddit validation failed.");
        }
    }

    public async Task<PlatformOperationResult> CreatePostAsync(
        string title,
        string caption,
        IReadOnlyList<string> images,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Reddit requires a title before publishing.");
        }

        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Reddit integration is not configured.");
        }

        var validation = await ValidateConnectionAsync(cancellationToken);
        if (!validation.Succeeded)
        {
            return validation;
        }

        var subreddit = (_settings.Current.RedditSubreddit ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(subreddit))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NotConfigured, "Reddit subreddit is not configured.");
        }

        var normalizedCaption = (caption ?? string.Empty).Trim();
        var mediaUrls = images?
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList() ?? [];

        if (mediaUrls.Count == 0)
        {
            return await SubmitSelfPostAsync(token, subreddit, normalizedTitle, normalizedCaption, cancellationToken);
        }

        var imageUrl = mediaUrls.FirstOrDefault(IsPublicHttpUrl);
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return PlatformOperationResult.Fail(
                PlatformOperationStatus.Limited,
                "Reddit requires a public image URL; local file uploads are not supported by the Reddit API.");
        }

        return await SubmitImagePostAsync(token, subreddit, normalizedTitle, normalizedCaption, imageUrl, cancellationToken);
    }

    public Task<PlatformOperationResult> UploadMediaAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Image path is empty."));
        }

        if (IsPublicHttpUrl(imagePath))
        {
            return Task.FromResult(PlatformOperationResult.Ok("Image URL is ready for Reddit submission.", externalId: imagePath));
        }

        if (File.Exists(imagePath))
        {
            return Task.FromResult(PlatformOperationResult.Fail(
                PlatformOperationStatus.Limited,
                "Reddit's official API does not support direct local image uploads. Use a public image URL instead."));
        }

        return Task.FromResult(PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Image file or URL was not found."));
    }

    private async Task<PlatformOperationResult> SubmitSelfPostAsync(string token, string subreddit, string title, string caption, CancellationToken cancellationToken)
    {
        try
        {
            var content = new Dictionary<string, string>
            {
                ["api_type"] = "json",
                ["kind"] = "self",
                ["sr"] = subreddit,
                ["title"] = title,
                ["text"] = caption
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/submit");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(content);

            var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailureAsync(response, "Reddit text post failed.", cancellationToken);
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var id = TryReadJsonField(raw, "id");
            return PlatformOperationResult.Ok("Reddit text post submitted.", id);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Reddit request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Reddit text post failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Reddit text post failed.");
        }
    }

    private async Task<PlatformOperationResult> SubmitImagePostAsync(string token, string subreddit, string title, string caption, string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            var content = new Dictionary<string, string>
            {
                ["api_type"] = "json",
                ["kind"] = "image",
                ["sr"] = subreddit,
                ["title"] = title,
                ["url"] = imageUrl,
                ["text"] = caption
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.reddit.com/api/submit");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(content);

            var response = await Http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailureAsync(response, "Reddit image post failed.", cancellationToken);
            }

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var id = TryReadJsonField(raw, "id");
            return PlatformOperationResult.Ok("Reddit image post submitted.", id);
        }
        catch (TaskCanceledException)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.NetworkFailure, "Reddit request timed out.");
        }
        catch (Exception ex)
        {
            _logger.Error("Reddit image post failed.", ex);
            return PlatformOperationResult.Fail(PlatformOperationStatus.Error, "Reddit image post failed.");
        }
    }

    private string ReadToken()
    {
        try
        {
            return string.IsNullOrWhiteSpace(_settings.Current.RedditAccessTokenProtected)
                ? string.Empty
                : _protector.Unprotect(_settings.Current.RedditAccessTokenProtected);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read Reddit token from secure settings.", ex);
            return string.Empty;
        }
    }

    private async Task<PlatformOperationResult> MapFailureAsync(HttpResponseMessage response, string fallbackMessage, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var lower = body.ToLowerInvariant();

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.AuthFailure, "Reddit token is invalid or expired.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests || lower.Contains("ratelimit") || lower.Contains("rate limit"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.RateLimited, "Reddit rate limit reached. Please retry later.");
        }

        if (lower.Contains("invalid subreddit") || lower.Contains("not found") || lower.Contains("invalid url"))
        {
            return PlatformOperationResult.Fail(PlatformOperationStatus.InvalidPermissions, "Reddit subreddit, permissions, or image URL are invalid.");
        }

        return PlatformOperationResult.Fail(PlatformOperationStatus.Error, string.IsNullOrWhiteSpace(body) ? fallbackMessage : body);
    }

    private static bool IsPublicHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
            if (document.RootElement.TryGetProperty("json", out var json) && json.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("id", out var idElement))
                {
                    return idElement.GetString() ?? string.Empty;
                }

                if (data.TryGetProperty("url", out var urlElement))
                {
                    return urlElement.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
