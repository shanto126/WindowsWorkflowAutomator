using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Utilities;

namespace WindowsWorkflowAutomator.Tests;

public class FoundationTests
{
    [Fact]
    public void AppPaths_UsesLocalAppData()
    {
        var paths = new AppPaths();

        Assert.Equal("WindowsWorkflowAutomator", AppPaths.ApplicationName);
        Assert.Contains("WindowsWorkflowAutomator", paths.RootDirectory);
        Assert.EndsWith("app.db", paths.DatabaseFilePath);
    }

    [Fact]
    public void Guard_Throws_WhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => Guard.NotNull<string>(null, "value"));
        Assert.Equal("ok", Guard.NotNull("ok", "value"));
    }
}
