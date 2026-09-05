namespace WindowsWorkflowAutomator.Authentication;

public sealed class AppSession
{
    public bool IsAuthenticated => UserId > 0;
    public int UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;

    public event EventHandler? LoggedOut;
    public event EventHandler? LoggedIn;

    public void SignIn(int userId, string role)
    {
        UserId = userId;
        Role = role;
        LoggedIn?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        UserId = 0;
        Role = string.Empty;
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }
}
