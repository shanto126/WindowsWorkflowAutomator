# Windows Workflow Automator

Windows Workflow Automator is a Windows desktop application that helps automate repetitive desktop tasks, manage files, schedule workflows, and orchestrate multi-platform social posts and GitHub automation — built as a phased university project with an industry-style architecture.

> Quick note: read [PROJECT_STATUS.md](PROJECT_STATUS.md) before making changes — it reflects the current phase and integration status.

## Highlights / Features

- Workflow Automation: create simple workflows composed of actions (open application, open folder, open website) and run them on demand or via scheduler.
- Task Scheduler: schedule saved workflows to run once, daily, or weekly. (Automatic scheduled execution is gated by license tier.)
- File Organizer: rule-based file moves/copies/renames with a download-folder monitor.
- GitHub Automation: configure a local repository, commit changes locally, and push manually; Smart Auto Sync (local auto-commit) is available (Premium gating applies).
- Social Media Manager: compose multi-platform drafts from an image folder, queue posts, and publish to supported platforms (Facebook, Instagram, YouTube, TikTok, Reddit, Threads). Some integrations are marked "Coming Soon" where noted.
- Licensing: local Free vs Premium license system (local activation, validation, and UI). Certain premium features are gated (automatic scheduler starts, Smart Auto Sync, workflow limits, etc.).
- Logging & persistence: EF Core + SQLite for local storage and a file-based application logger.

## Tech stack

- .NET 8 (WinForms)
- C# 12
- Microsoft.Extensions.Hosting / DI
- EF Core (SQLite provider)
- xUnit for tests

## Current status (short)

See detailed phase status in PROJECT_STATUS.md. Key points:
- Social Media Manager, GitHub automation, Workflow engine, Scheduler, and File Organizer are implemented.
- Local license service and UI are present; premium gating logic for scheduler/auto-sync/workflow limits has been added.
- Tests are present and passing locally.

## Getting started (developer)

Prerequisites:
- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022 (17.8+) with the .NET desktop workload

Open and run:
1. git clone <repo>
2. cd WindowsWorkflowAutomator
3. dotnet build WindowsWorkflowAutomator.sln
4. dotnet test WindowsWorkflowAutomator.sln
5. dotnet run --project Source/WindowsWorkflowAutomator

Or open `WindowsWorkflowAutomator.sln` in Visual Studio and run the `WindowsWorkflowAutomator` startup project.

## Configuration and platform credentials

Several integrations require API credentials or tokens. Place these in the Settings page in the application UI or in the local configuration when available. Summary:

- Facebook / Instagram (Meta): App ID, Page ID, and Page access token. The token is stored encrypted locally. If missing, the app will show "not configured" and block publish operations.
- YouTube: OAuth 2.0 access token — required for uploads via the YouTube Data API.
- TikTok / Reddit / Threads: OAuth or bearer tokens depending on platform — each platform page provides configuration guidance.
- GitHub: Personal Access Token for push operations (stored encrypted). Local path to repository and remote URL are required for Smart Auto Sync.

See the platform-specific README or the corresponding page under `UI/Pages` for details and examples of required scopes and settings.

## Developer notes & architecture

- Separation of concerns: UI pages (UserControl) call into service layer classes — business logic should live under `Services/` or dedicated feature folders.
- Database schema: `AppDbContext` sets up tables via `EnsureSchema()` on first run; LicenseInfos, Workflows, ScheduledTasks, SocialPosts, etc., are created automatically.
- Feature gating: the LicenseService is used at startup and in key UI paths to gate premium features locally.

## Tests

- Tests live under `Tests/WindowsWorkflowAutomator.Tests` (xUnit). Run all tests with `dotnet test`.
- New tests include Licensing repository/service validation and workflow automation tests.

## How to build a Release package (local)

To produce a Release build and publish the app (non-self-contained):

```powershell
dotnet publish Source/WindowsWorkflowAutomator -c Release -r win-x64 --self-contained false -o publish\win-x64
```

The produced executable and DLLs will be in `publish\win-x64`.

## Contribution / Team

Primary contributors for this repo:
- Shanto — core architecture, GitHub automation, Social Media multi-platform orchestration
- Emam — Workflow Automation and Scheduler
- Toriqul — Dashboard and UI polish

When contributing:
1. Read PROJECT_STATUS.md to determine the current phase and owner.
2. Make focused changes per-phase; commit locally (one commit per focused step) and do not push without reviewer approval.
3. Keep UI polish separate from business-logic changes.

## Known limitations & future work

- AI-assisted captions use a built-in, local caption assistant. It turns a short brief into a ready-to-edit caption without sending data to an external service; a cloud-backed generative AI provider is not included.
- Some adapters (LinkedIn, Snapchat) are intentionally marked "Coming Soon" and will block publishing with an explanatory message.
- Device-limited licensing fields are present but multi-device flows are not yet implemented.
- Dashboard summary cards are now wired to live DB counts (workflows, scheduled tasks, queued social posts, license status).

## Demo checklist (quick)

- Run `dotnet run --project Source/WindowsWorkflowAutomator`.
- Settings → License: activate or test license key (format `WFA-PRO-XXXX-YYYY`).
- Dashboard → verify counts (workflows, scheduled tasks, social queue).
- File Organizer → create a rule and test organizing a file.
- Workflow Automation → create a workflow and Run Now.
- Task Scheduler → add a schedule and test Run Now (automatic runs require Premium license).
- GitHub → configure a repo, commit locally, and push manually (push requires PAT and remote).

## License

This project is a course/university project and not intended as a commercial product.

---

If you want, I can now:
- show full diffs of the committed files (the actual patch contents),
- commit an improved README (done) and push it if you instruct (I will not push without your explicit go-ahead),
- produce a detailed pre-demo checklist with sample credentials and exact button paths for the demo.
