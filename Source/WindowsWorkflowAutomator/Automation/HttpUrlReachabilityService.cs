namespace WindowsWorkflowAutomator.Automation;

public sealed class HttpUrlReachabilityService : IUrlReachabilityService, IDisposable
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public async Task EnsureReachableAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await _http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if ((int)response.StatusCode >= 400)
        {
            throw new InvalidOperationException($"Website returned HTTP {(int)response.StatusCode}: {uri}");
        }
    }

    public void Dispose() => _http.Dispose();
}
