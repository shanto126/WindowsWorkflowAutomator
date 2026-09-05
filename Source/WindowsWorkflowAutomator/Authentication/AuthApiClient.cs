using System.Net.Http.Json;

namespace WindowsWorkflowAutomator.Authentication;

public sealed class AuthApiClient(HttpClient httpClient)
{
    public async Task<LoginResult?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken);
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResult(bool Success, string Role, int UserId);
