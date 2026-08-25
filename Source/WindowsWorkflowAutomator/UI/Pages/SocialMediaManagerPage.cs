using WindowsWorkflowAutomator.Configuration;
using WindowsWorkflowAutomator.Logging;
using WindowsWorkflowAutomator.Models;
using WindowsWorkflowAutomator.SocialMedia;
using WindowsWorkflowAutomator.SocialMedia.Adapters;

namespace WindowsWorkflowAutomator.UI.Pages;

public sealed class SocialMediaManagerPage : UserControl
{
    private readonly ISocialMediaService _socialMedia;
    private readonly IFacebookService _facebook;
    private readonly ILinkedInService _linkedIn;
    private readonly IAppSettingsService _settings;
    private readonly IAppLogger _logger;
    private readonly IEnumerable<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter> _platformAdapters;
    private readonly MultiPlatformPostOrchestrator _postOrchestrator;

    private readonly TextBox _folderBox = new();
    private readonly FlowLayoutPanel _platformStatusPanel = new();
    private readonly NumericUpDown _postCount = new();
    private readonly NumericUpDown _imagesPerPost = new();
    private readonly CheckedListBox _platformList = new();
    private readonly ComboBox _captionModeBox = new();
    private readonly TextBox _captionBox = new();
    private readonly TextBox _composeTitleBox = new();
    private readonly TextBox _composeCaptionBox = new();
    private readonly TextBox _composeHashtagsBox = new();
    private readonly TextBox _facebookAppIdBox = new();
    private readonly TextBox _facebookPageIdBox = new();
    private readonly TextBox _facebookTokenBox = new();
    private readonly Label _facebookStatus = new();
    private readonly Label _platformNote = new();
    private readonly ListBox _activity = new();
    private readonly DataGridView _queueGrid = new();

    private readonly BindingSource _queueBinding = new();
    private List<SocialPost> _queue = [];

    public SocialMediaManagerPage(
        ISocialMediaService socialMedia,
        IFacebookService facebook,
        ILinkedInService linkedIn,
        IAppSettingsService settings,
        IAppLogger logger,
        IEnumerable<WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter> platformAdapters,
        MultiPlatformPostOrchestrator postOrchestrator)
    {
        _socialMedia = socialMedia;
        _facebook = facebook;
        _linkedIn = linkedIn;
        _settings = settings;
        _logger = logger;
        _platformAdapters = platformAdapters;
        _postOrchestrator = postOrchestrator;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 252);
        Padding = new Padding(24);

        BuildLayout();
        Load += OnLoad;
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            Text = "Social Media Manager",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Dock = DockStyle.Top
        };

        var subtitle = new Label
        {
            Text = "Create draft posts from an image folder, manage queue status, and publish Facebook posts manually.",
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(75, 85, 99),
            Padding = new Padding(0, 4, 0, 12),
            MaximumSize = new Size(980, 0),
            Dock = DockStyle.Top
        };

        // Platform status panel (polished UI): shows supported/coming-soon and configured state
        _platformStatusPanel.Dock = DockStyle.Top;
        _platformStatusPanel.Height = 84;
        _platformStatusPanel.Padding = new Padding(0, 8, 0, 4);
        _platformStatusPanel.AutoSize = false;
        _platformStatusPanel.WrapContents = true;
        _platformStatusPanel.AutoScroll = true;
        _postCount.Minimum = 1;
        _postCount.Maximum = 100;
        _postCount.Value = 3;

        _imagesPerPost.Minimum = 1;
        _imagesPerPost.Maximum = 20;
        _imagesPerPost.Value = 2;

        _platformList.CheckOnClick = true;
        _platformList.Height = 104;

        // Platforms will be populated during OnLoad from registered adapters
        _platformList.ItemCheck += (_, _) => UpdatePlatformState();

        _captionModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _captionModeBox.Items.Add(CaptionMode.Manual);
        _captionModeBox.Items.Add(CaptionMode.Template);
        _captionModeBox.Items.Add(CaptionMode.AiAssisted);
        _captionModeBox.SelectedItem = CaptionMode.Manual;

        _captionBox.Multiline = true;
        _captionBox.Height = 84;
        _captionBox.ScrollBars = ScrollBars.Vertical;
        _captionBox.PlaceholderText = "Write your caption here. For AI-assisted mode, write a short idea such as: New product launch";
        _captionBox.Text = "New post #{Index}/{Total} on {Platform} ({Date})";

        _facebookTokenBox.UseSystemPasswordChar = true;

        var setup = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 8)
        };
        setup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        setup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        setup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddField(setup, 0, "Image folder", _folderBox, CreateButton("Browse…", OnBrowseFolder));
        AddField(setup, 1, "Post count", _postCount);
        AddField(setup, 2, "Images per post", _imagesPerPost);
        AddField(setup, 3, "Platforms", _platformList);
        AddField(setup, 4, "Caption mode", _captionModeBox);
        AddField(setup, 5, "Caption / template / idea", _captionBox);

        _platformNote.Text = string.Empty;
        _platformNote.AutoSize = true;
        _platformNote.ForeColor = Color.FromArgb(180, 83, 9);
        _platformNote.Visible = false;
        _platformNote.Dock = DockStyle.Top;

        _composeTitleBox.PlaceholderText = "Post title (required for Reddit / YouTube)";
        _composeCaptionBox.Multiline = true;
        _composeCaptionBox.Height = 70;
        _composeCaptionBox.ScrollBars = ScrollBars.Vertical;
        _composeCaptionBox.Text = "New product update for today.";
        _composeHashtagsBox.PlaceholderText = "#summer #launch #social";

        var unifiedTitle = new Label
        {
            Text = "Unified compose",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };

        var unifiedCompose = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 8)
        };
        unifiedCompose.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        unifiedCompose.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        unifiedCompose.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

        AddField(unifiedCompose, 0, "Title", _composeTitleBox);
        AddField(unifiedCompose, 1, "Caption", _composeCaptionBox);
        AddField(unifiedCompose, 2, "Hashtags", _composeHashtagsBox);

        var unifiedButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        var unifiedPublish = CreateButton("Post now to selected platforms", OnUnifiedPostNow);
        unifiedPublish.BackColor = Color.FromArgb(22, 163, 74);
        unifiedButtons.Controls.Add(unifiedPublish);

        var draftButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        draftButtons.Controls.Add(CreateButton("1. Create drafts", OnCreateDrafts));
        draftButtons.Controls.Add(CreateButton("Queue selected", OnQueueSelected));
        draftButtons.Controls.Add(CreateButton("Edit caption", OnEditSelected));
        draftButtons.Controls.Add(CreateButton("Cancel", OnCancelSelected));
        draftButtons.Controls.Add(CreateButton("Retry", OnRetrySelected));

        var publishButton = CreateButton("2. Publish selected draft", OnPublishNow);
        publishButton.BackColor = Color.FromArgb(22, 163, 74);
        draftButtons.Controls.Add(publishButton);

        var facebookTitle = new Label
        {
            Text = "Facebook connection",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI Semibold", 10.5F),
            Padding = new Padding(0, 6, 0, 0)
        };

        var facebookConfig = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 0, 8)
        };
        facebookConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        facebookConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        facebookConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        AddField(facebookConfig, 0, "Facebook App ID", _facebookAppIdBox);
        AddField(facebookConfig, 1, "Facebook Page ID", _facebookPageIdBox);
        AddField(facebookConfig, 2, "Access token", _facebookTokenBox);

        var fbButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        fbButtons.Controls.Add(CreateButton("Connect Facebook", OnConnectFacebook));
        fbButtons.Controls.Add(CreateButton("Validate", OnValidateFacebook));
        fbButtons.Controls.Add(CreateButton("Disconnect", OnDisconnectFacebook));

        _facebookStatus.Text = "Facebook integration is not configured.";
        _facebookStatus.Dock = DockStyle.Top;
        _facebookStatus.ForeColor = Color.FromArgb(180, 83, 9);
        _facebookStatus.Padding = new Padding(0, 0, 0, 8);

        _queueGrid.Dock = DockStyle.Fill;
        _queueGrid.ReadOnly = true;
        _queueGrid.AllowUserToAddRows = false;
        _queueGrid.AllowUserToDeleteRows = false;
        _queueGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _queueGrid.MultiSelect = true;
        _queueGrid.RowHeadersVisible = false;
        _queueGrid.AutoGenerateColumns = false;
        _queueGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _queueGrid.BackgroundColor = Color.White;
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.PostId), HeaderText = "Post ID", FillWeight = 18 });
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.Platform), HeaderText = "Platform", FillWeight = 12 });
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.Status), HeaderText = "Status", FillWeight = 12 });
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.Images), HeaderText = "Images", FillWeight = 8 });
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.Caption), HeaderText = "Caption", FillWeight = 34 });
        _queueGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(QueueRow.Error), HeaderText = "Last Error", FillWeight = 16 });
        _queueGrid.DataSource = _queueBinding;

        var queueTitle = new Label
        {
            Text = "Post queue",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };

        _activity.Dock = DockStyle.Fill;
        _activity.Font = new Font("Consolas", 9F);

        var activityTitle = new Label
        {
            Text = "Activity",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI Semibold", 11F),
            Padding = new Padding(0, 8, 0, 0)
        };

        var lowerSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260,
            Panel1MinSize = 200,
            Panel2MinSize = 120
        };

        var queuePanel = new Panel { Dock = DockStyle.Fill };
        queuePanel.Controls.Add(_queueGrid);
        queuePanel.Controls.Add(queueTitle);

        var activityPanel = new Panel { Dock = DockStyle.Fill };
        activityPanel.Controls.Add(_activity);
        activityPanel.Controls.Add(activityTitle);

        lowerSplit.Panel1.Controls.Add(queuePanel);
        lowerSplit.Panel2.Controls.Add(activityPanel);

        var setupPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 0, 0, 8) };
        setupPanel.Controls.Add(_facebookStatus);
        setupPanel.Controls.Add(fbButtons);
        setupPanel.Controls.Add(facebookConfig);
        setupPanel.Controls.Add(facebookTitle);
        setupPanel.Controls.Add(unifiedButtons);
        setupPanel.Controls.Add(unifiedCompose);
        setupPanel.Controls.Add(unifiedTitle);
        setupPanel.Controls.Add(draftButtons);
        setupPanel.Controls.Add(_platformNote);
        setupPanel.Controls.Add(setup);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 360));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(setupPanel, 0, 0);
        body.Controls.Add(lowerSplit, 0, 1);

        Controls.Add(body);
        Controls.Add(_platformStatusPanel);
        Controls.Add(subtitle);
        Controls.Add(title);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        _facebookAppIdBox.Text = _settings.Current.FacebookAppId;
        _facebookPageIdBox.Text = _settings.Current.FacebookPageId;
        _facebookTokenBox.PlaceholderText = string.IsNullOrWhiteSpace(_settings.Current.FacebookAccessTokenProtected)
            ? string.Empty
            : "Token already stored";

        UpdatePlatformState();
        PopulatePlatforms();
        PopulatePlatformStatus();
        await RefreshQueueAsync();
        await RefreshFacebookStatusAsync();
    }

    private void UpdatePlatformState()
    {
        // Show a note if any selected platform is Coming Soon
        var anyComingSoon = _platformList.CheckedItems.Cast<object>()
            .OfType<PlatformItem>()
            .Any(item => !item.Supported);
        _platformNote.Visible = anyComingSoon;
        _platformNote.Text = anyComingSoon ? "One or more selected platforms are coming soon and cannot publish." : string.Empty;
    }

    private void PopulatePlatformStatus()
    {
        _platformStatusPanel.Controls.Clear();
        if (_platformAdapters is null)
        {
            return;
        }

        foreach (var adapter in _platformAdapters.OrderBy(a => a.DisplayName))
        {
            var lbl = new Label
            {
                Text = adapter.DisplayName,
                AutoSize = false,
                Width = 125,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0, 0, 8, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            // Status badge
            var badge = new Label
            {
                AutoSize = false,
                Width = 88,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right,
                Margin = new Padding(8, 5, 0, 5),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };

            if (!adapter.IsSupported)
            {
                badge.Text = "Coming Soon";
                badge.BackColor = Color.FromArgb(245, 158, 11);
                badge.ForeColor = Color.White;
            }
            else
            {
                // Default to Not Configured; call ValidateConnectionAsync asynchronously to update the badge
                badge.Text = "Not configured";
                badge.BackColor = Color.FromArgb(239, 68, 68);
                badge.ForeColor = Color.White;

                // Fire-and-forget validation to update badge
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await adapter.ValidateConnectionAsync();
                        if (_platformStatusPanel.InvokeRequired) _platformStatusPanel.Invoke(new Action(() =>
                        {
                            if (result.Succeeded)
                            {
                                badge.Text = "Configured";
                                badge.BackColor = Color.FromArgb(16, 185, 129);
                            }
                            else
                            {
                                badge.Text = result.Message ?? "Not configured";
                                badge.BackColor = Color.FromArgb(239, 68, 68);
                            }
                                                }));
                                                else
                                                {
                                                    if (result.Succeeded)
                                                    {
                                                        badge.Text = "Configured";
                                                        badge.BackColor = Color.FromArgb(16, 185, 129);
                                                    }
                                                    else
                                                    {
                                                        badge.Text = result.Message ?? "Not configured";
                                                        badge.BackColor = Color.FromArgb(239, 68, 68);
                                                    }
                                                }
                    }
                    catch
                    {
                        if (_platformStatusPanel.InvokeRequired) _platformStatusPanel.Invoke(new Action(() =>
                        {
                            badge.Text = "Error";
                            badge.BackColor = Color.FromArgb(107, 114, 128);
                                                }));
                                                else
                                                {
                                                    badge.Text = "Error";
                                                    badge.BackColor = Color.FromArgb(107, 114, 128);
                                                }
                    }
                });
            }

            var card = new Panel
            {
                Width = 225,
                Height = 36,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(8)
            };
            lbl.Dock = DockStyle.Left;
            badge.Dock = DockStyle.Right;
            card.Controls.Add(lbl);
            card.Controls.Add(badge);
            _platformStatusPanel.Controls.Add(card);
        }
    }
    private IReadOnlyList<SocialPlatform> GetSelectedPlatforms()
    {
        return _platformList.CheckedItems.Cast<object>()
            .OfType<PlatformItem>()
            .Select(i => i.Platform)
            .ToList();
    }

    private void PopulatePlatforms()
    {
        _platformList.Items.Clear();

        var visiblePlatforms = _platformAdapters?
            .ToDictionary(adapter => adapter.Platform, adapter => adapter)
            ?? new Dictionary<SocialPlatform, WindowsWorkflowAutomator.SocialMedia.Adapters.ISocialPlatformAdapter>();

        foreach (var platform in Enum.GetValues<SocialPlatform>())
        {
            var adapter = visiblePlatforms.TryGetValue(platform, out var existing)
                ? existing
                : new ComingSoonPlatformAdapter(platform, GetFallbackPlatformLabel(platform));

            var item = new PlatformItem(adapter.Platform, adapter.DisplayName, adapter.IsSupported);
            _platformList.Items.Add(item, platform == SocialPlatform.Facebook);
        }
    }

    private static string GetFallbackPlatformLabel(SocialPlatform platform) => platform switch
    {
        SocialPlatform.Facebook => "Facebook",
        SocialPlatform.LinkedIn => "LinkedIn (Coming Soon)",
        SocialPlatform.Instagram => "Instagram",
        SocialPlatform.X => "X / Twitter (Coming Soon)",
        SocialPlatform.YouTube => "YouTube",
        SocialPlatform.TikTok => "TikTok",
        SocialPlatform.Reddit => "Reddit",
        SocialPlatform.Threads => "Threads",
        SocialPlatform.Snapchat => "Snapchat (Coming Soon)",
        _ => platform.ToString()
    };

    private CaptionMode GetCaptionMode() =>
        _captionModeBox.SelectedItem is CaptionMode mode ? mode : CaptionMode.Manual;

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose an image folder",
            UseDescriptionForTitle = true,
            SelectedPath = _folderBox.Text
        };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private async void OnCreateDrafts(object? sender, EventArgs e)
    {
        try
        {
            var selectedPlatforms = GetSelectedPlatforms();
            var mode = GetCaptionMode();

            if (selectedPlatforms.Count == 0)
            {
                ShowWarning("Select at least one platform to create drafts for.");
                return;
            }

            var totalCreated = 0;
            foreach (var platform in selectedPlatforms)
            {
                // If platform is coming soon, skip creating drafts but inform the user
                var adapter = _platformAdapters.FirstOrDefault(a => a.Platform == platform);
                if (adapter is not null && !adapter.IsSupported)
                {
                    AppendActivity($"Skipped {platform}: coming soon.");
                    continue;
                }

                var created = await _socialMedia.CreateDraftsFromFolderAsync(new SocialFolderDraftRequest
                {
                    FolderPath = _folderBox.Text,
                    Platform = platform,
                    PostCount = (int)_postCount.Value,
                    ImagesPerPost = (int)_imagesPerPost.Value,
                    CaptionMode = mode,
                    CaptionInput = _captionBox.Text
                });

                totalCreated += created.Count;
            }

            AppendActivity($"Created {totalCreated} draft post(s).");
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Draft generation failed.", ex);
            ShowWarning(ex.Message);
        }
    }

    private async void OnQueueSelected(object? sender, EventArgs e)
    {
        var ids = GetSelectedPostIds();
        if (ids.Count == 0)
        {
            ShowWarning("Select at least one post to queue.");
            return;
        }

        var queued = await _socialMedia.SchedulePostAsync(ids);
        AppendActivity($"Queued {queued.Count} post(s) as pending/scheduled.");
        await RefreshQueueAsync();
    }

    private async void OnEditSelected(object? sender, EventArgs e)
    {
        var post = GetSelectedPost();
        if (post is null)
        {
            ShowWarning("Select one post to edit.");
            return;
        }

        using var dialog = new CaptionEditDialog(post.CaptionMode, post.CaptionInput);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var result = await _socialMedia.UpdateCaptionAsync(post.Id, dialog.Mode, dialog.CaptionText);
            if (!result.Succeeded)
            {
                ShowWarning(result.Message);
            }

            AppendActivity(result.Message);
            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private async void OnCancelSelected(object? sender, EventArgs e)
    {
        var ids = GetSelectedPostIds();
        if (ids.Count == 0)
        {
            ShowWarning("Select at least one post to cancel.");
            return;
        }

        foreach (var id in ids)
        {
            var result = await _socialMedia.CancelPostAsync(id);
            AppendActivity(result.Message);
        }

        await RefreshQueueAsync();
    }

    private async void OnRetrySelected(object? sender, EventArgs e)
    {
        var ids = GetSelectedPostIds();
        if (ids.Count == 0)
        {
            ShowWarning("Select at least one post to retry.");
            return;
        }

        foreach (var id in ids)
        {
            var result = await _socialMedia.RetryPostAsync(id);
            AppendActivity(result.Message);
            if (!result.Succeeded)
            {
                ShowWarning(result.Message);
            }
        }

        await RefreshQueueAsync();
    }

    private async void OnPublishNow(object? sender, EventArgs e)
    {
        var post = GetSelectedPost();
        if (post is null)
        {
            ShowWarning("Select one post to publish.");
            return;
        }

        // Determine adapter for the post's platform
        var adapter = _platformAdapters.FirstOrDefault(a => a.Platform == post.Platform);
        if (adapter is null)
        {
            ShowWarning("No adapter available for the selected platform.");
            return;
        }

        if (!adapter.IsSupported)
        {
            var coming = PlatformOperationResult.Fail(PlatformOperationStatus.ComingSoon, "This platform is coming soon and cannot publish.");
            ShowWarning(coming.Message);
            AppendActivity(coming.Message);
            return;
        }

        await _socialMedia.SetProcessingAsync(post.Id);
        await RefreshQueueAsync();

        var publish = await adapter.CreatePostAsync(post.ResolvedCaption, post.Images.Select(x => x.FilePath).ToArray());
        if (publish.Succeeded)
        {
            await _socialMedia.MarkPublishedAsync(post.Id);
            AppendActivity($"{adapter.DisplayName} post published successfully.");
        }
        else
        {
            await _socialMedia.MarkFailedAsync(post.Id, publish.Message);
            ShowWarning(publish.Message);
            AppendActivity($"Publish failed: {publish.Message}");
        }

        await RefreshQueueAsync();
        // Refresh Facebook status if adapter is Facebook
        if (adapter.Platform == SocialPlatform.Facebook)
        {
            await RefreshFacebookStatusAsync();
        }
    }

    private async void OnUnifiedPostNow(object? sender, EventArgs e)
    {
        var selectedPlatforms = GetSelectedPlatforms()
            .Where(platform => platform != SocialPlatform.LinkedIn && platform != SocialPlatform.X && platform != SocialPlatform.Snapchat)
            .Distinct()
            .ToList();

        if (selectedPlatforms.Count == 0)
        {
            ShowWarning("Select at least one configured, non-Coming-Soon platform.");
            return;
        }

        var folderPath = _folderBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            ShowWarning("Select a valid image folder before posting.");
            return;
        }

        var request = new MultiPlatformComposeRequest(
            selectedPlatforms,
            folderPath,
            _composeTitleBox.Text.Trim(),
            _composeCaptionBox.Text.Trim(),
            _composeHashtagsBox.Text.Trim(),
            (int)_postCount.Value,
            (int)_imagesPerPost.Value);

        try
        {
            var results = await _postOrchestrator.PublishAsync(request);
            foreach (var result in results)
            {
                var label = result.Succeeded ? "Published" : result.Status == PlatformOperationStatus.NotConfigured ? "Configuration Required" : "Failed";
                AppendActivity($"{result.Platform}: {label} - {result.Message}");
            }

            if (results.Count == 0)
            {
                ShowWarning("Select at least one supported platform for the unified post.");
                return;
            }

            var failed = results.Count(x => !x.Succeeded && x.Status != PlatformOperationStatus.NotConfigured && x.Status != PlatformOperationStatus.ComingSoon);
            if (failed > 0)
            {
                ShowWarning($"{failed} platform(s) failed to publish. Check the activity log for details.");
            }

            await RefreshQueueAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Unified post orchestration failed.", ex);
            ShowWarning(ex.Message);
        }
    }

    private async void OnConnectFacebook(object? sender, EventArgs e)
    {
        _settings.Current.FacebookAppId = _facebookAppIdBox.Text.Trim();
        _settings.Current.FacebookPageId = _facebookPageIdBox.Text.Trim();
        _settings.Save();

        var token = string.IsNullOrWhiteSpace(_facebookTokenBox.Text)
            ? null
            : _facebookTokenBox.Text;

        var result = await _facebook.ConnectAsync(token);
        _facebookTokenBox.Text = string.Empty;
        _facebookTokenBox.PlaceholderText = string.IsNullOrWhiteSpace(_settings.Current.FacebookAccessTokenProtected)
            ? string.Empty
            : "Token already stored";

        ApplyFacebookStatus(result);
    }

    private async void OnValidateFacebook(object? sender, EventArgs e)
    {
        _settings.Current.FacebookAppId = _facebookAppIdBox.Text.Trim();
        _settings.Current.FacebookPageId = _facebookPageIdBox.Text.Trim();
        _settings.Save();

        var result = await _facebook.ValidateConnectionAsync();
        ApplyFacebookStatus(result);
    }

    private async void OnDisconnectFacebook(object? sender, EventArgs e)
    {
        var result = await _facebook.DisconnectAsync();
        ApplyFacebookStatus(result);
    }

    private async Task RefreshQueueAsync()
    {
        _queue = [.. await _socialMedia.GetQueueAsync()];
        var rows = _queue
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new QueueRow
            {
                PostId = x.Id,
                Platform = x.Platform.ToString(),
                Status = x.Status.ToString(),
                Images = x.Images.Count,
                Caption = x.ResolvedCaption,
                Error = x.ErrorMessage ?? string.Empty
            })
            .ToList();

        _queueBinding.DataSource = rows;
        _queueBinding.ResetBindings(false);
    }

    private async Task RefreshFacebookStatusAsync()
    {
        var result = await _facebook.ValidateConnectionAsync();
        ApplyFacebookStatus(result, showDialogOnError: false);
    }

    private void ApplyFacebookStatus(PlatformOperationResult result, bool showDialogOnError = true)
    {
        _facebookStatus.Text = result.Message;
        _facebookStatus.ForeColor = result.Succeeded
            ? Color.FromArgb(21, 128, 61)
            : Color.FromArgb(180, 83, 9);

        AppendActivity($"Facebook: {result.Message}");
        if (!result.Succeeded && showDialogOnError)
        {
            ShowWarning(result.Message);
        }
    }

    private List<Guid> GetSelectedPostIds()
    {
        var ids = new List<Guid>();
        foreach (DataGridViewRow row in _queueGrid.SelectedRows)
        {
            if (row.DataBoundItem is QueueRow item)
            {
                ids.Add(item.PostId);
            }
        }

        return ids;
    }

    private SocialPost? GetSelectedPost()
    {
        if (_queueGrid.SelectedRows.Count != 1)
        {
            return null;
        }

        if (_queueGrid.SelectedRows[0].DataBoundItem is not QueueRow row)
        {
            return null;
        }

        return _queue.FirstOrDefault(x => x.Id == row.PostId);
    }

    private void AppendActivity(string message)
    {
        _activity.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (_activity.Items.Count > 120)
        {
            _activity.Items.RemoveAt(_activity.Items.Count - 1);
        }
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(FindForm(), message, "Social Media Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private static void AddField(
        TableLayoutPanel host,
        int row,
        string label,
        Control editor,
        Control? sideButton = null)
    {
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var caption = new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, 7, 12, 4)
        };

        editor.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        editor.Margin = new Padding(0, 4, 8, 4);

        host.Controls.Add(caption, 0, row);
        host.Controls.Add(editor, 1, row);

        if (sideButton is not null)
        {
            sideButton.Margin = new Padding(0, 4, 0, 4);
            host.Controls.Add(sideButton, 2, row);
        }
        else
        {
            host.Controls.Add(new Panel { Width = 1 }, 2, row);
        }
    }

    private sealed record PlatformChoice(SocialPlatform Value, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed class QueueRow
    {
        public Guid PostId { get; init; }

        public string Platform { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public int Images { get; init; }

        public string Caption { get; init; } = string.Empty;

        public string Error { get; init; } = string.Empty;
    }

    private sealed class CaptionEditDialog : Form
    {
        private readonly ComboBox _modeBox = new();
        private readonly TextBox _captionBox = new();

        public CaptionEditDialog(CaptionMode mode, string caption)
        {
            Text = "Edit caption";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(460, 220);

            _modeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeBox.Items.Add(CaptionMode.Manual);
            _modeBox.Items.Add(CaptionMode.Template);
            _modeBox.Items.Add(CaptionMode.AiAssisted);
            _modeBox.SelectedItem = mode;
            _modeBox.Dock = DockStyle.Top;

            _captionBox.Multiline = true;
            _captionBox.Dock = DockStyle.Fill;
            _captionBox.Text = caption;

            var ok = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(_captionBox);
            Controls.Add(_modeBox);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public CaptionMode Mode => _modeBox.SelectedItem is CaptionMode mode ? mode : CaptionMode.Manual;

        public string CaptionText => _captionBox.Text;
    }
}
