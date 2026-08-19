using WindowsWorkflowAutomator.SocialMedia;

namespace WindowsWorkflowAutomator.Tests;

public sealed class SocialMediaPlannerTests
{
    [Fact]
    public void SplitImagePaths_Groups_ByRequestedCount()
    {
        var images = Enumerable.Range(1, 8)
            .Select(i => $"img-{i:00}.jpg")
            .ToArray();

        var groups = SocialPostPlanner.SplitImagePaths(images, postCount: 4, imagesPerPost: 2);

        Assert.Equal(4, groups.Count);
        Assert.All(groups, group => Assert.Equal(2, group.Count));
        Assert.Equal("img-01.jpg", groups[0][0]);
        Assert.Equal("img-08.jpg", groups[3][1]);
    }

    [Fact]
    public void SplitImagePaths_Throws_WhenInsufficientImages()
    {
        var images = new[] { "a.jpg", "b.jpg", "c.jpg" };

        var error = Assert.Throws<InvalidOperationException>(() =>
            SocialPostPlanner.SplitImagePaths(images, postCount: 2, imagesPerPost: 2));

        Assert.Contains("Not enough images", error.Message);
    }

    [Fact]
    public void CaptionTemplateRenderer_ReplacesPlaceholders()
    {
        var caption = CaptionTemplateRenderer.Render(
            "Post {Index}/{Total} on {Platform} ({Date})",
            new Dictionary<string, string>
            {
                ["Index"] = "2",
                ["Total"] = "5",
                ["Platform"] = "Facebook",
                ["Date"] = "2026-08-19"
            });

        Assert.Equal("Post 2/5 on Facebook (2026-08-19)", caption);
    }
}
