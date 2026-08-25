Demo checklist — Windows Workflow Automator

This document is intended to guide the demo flow and includes the exact steps and credentials (replace placeholders) to exercise the main app features.

1) Local pre-checks
   - dotnet build WindowsWorkflowAutomator.sln  -> must pass
   - dotnet test WindowsWorkflowAutomator.sln   -> must pass (all tests)
   - Release package is available at `publish\win-x64` (requires the .NET 8 Desktop Runtime on the demo machine).

2) Start app
   - Run in debugger (Visual Studio) or run: dotnet run --project Source/WindowsWorkflowAutomator

3) License (Settings page)
   - Open Settings -> License
   - Show current tier (Free)
   - Enter a sample Premium license key (format: WFA-PRO-XXXX-YYYY), e.g. WFA-PRO-ABCD-1234
   - Click Activate -> Confirm activation message and "Current tier: Premium"

4) File Organizer
   - Open File Organizer, create a rule: extension `.txt` -> Move -> Destination folder: C:\Temp\wwa-organized
   - Enable monitoring or click Organize now
   - Drop a test file into the watched folder and confirm it moves to the destination and activity log updated

5) Workflow Automation
   - Open Workflow Automation page
   - Create a workflow named "Demo Morning" with three actions: Open Application (point to a small exe), Open Website (https://example.com), Open Folder
   - Save and click Run Now -> verify actions executed (or simulate if environment restricted)

6) Task Scheduler
   - Open Task Scheduler, Add schedule for the workflow created above
   - Use Run Now to show immediate execution
   - Explain that automatic scheduled runs require Premium license (show Settings to verify Premium). If license is Free, the app will not start the scheduler timer automatically.

7) GitHub Automation
   - Open GitHub Automation page
   - Configure Local repository path, Remote URL, Branch, Commit template, and Personal Access Token (PAT)
   - Save configuration and test Refresh status
   - Click Commit (local) and Push to GitHub (confirm push requires PAT and user confirmation)
   - Try to enable Smart Auto Sync on Free tier to show the warning dialog; then demonstrate enabling it after switching to Premium.

8) Social Media Manager
   - Configure Facebook (AppId/PageId/Token) or another configured platform
   - Create drafts from an image folder, queue posts, and Publish one post manually
   - Select AI-assisted caption mode, enter a short brief, and create drafts. Show the generated, editable caption; it works locally and requires no API credentials.

9) Wrap up
   - Show logs: %LocalAppData%\WindowsWorkflowAutomator\
   - Show DB file location and explain tables (Workflows, ScheduledTasks, LicenseInfos, SocialPosts)

Notes and placeholders
- Replace sample license key and tokens with real test credentials before demo.
- If you don't have platform credentials for the demo, show the UI screens and explain required configuration and scopes.

Done — do not push changes automatically. Commit created locally: DEMO_CHECKLIST.md
