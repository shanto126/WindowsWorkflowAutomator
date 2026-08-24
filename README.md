# Windows Workflow Automator

A Windows desktop application for organizing everyday computer work: launching apps and websites, automating simple workflows, organizing files, scheduling tasks, and keeping activity logs.

This repository is a **university C# project** with an industry-style layout. Features are added in phases. Phase 1 (foundation) and Phase 3 (File Organizer) are in place.

Team members: read **[PROJECT_STATUS.md](PROJECT_STATUS.md)** at the start of every session so you know the current phase, owner, and next step.

## Current status

Phase 1 — architecture and GUI shell:

- Visual Studio 2022 compatible WinForms app on **.NET 8**
- Layered folders (UI, services, data, repositories)
- SQLite + Entity Framework Core for local storage
- Dependency injection via `Microsoft.Extensions.Hosting`
- File logging and JSON user settings
- Main window with sidebar navigation and placeholder module pages

Phase 3 — File Organizer:

- Extension-based rules (move / copy / rename)
- Download folder monitor (`FileSystemWatcher`)
- File Organizer page: folder picker, rule list, monitoring toggle, activity log
- Task Scheduler: schedule saved workflows once, daily, or weekly with background execution

Other feature modules (workflow engine, GitHub, Facebook, license keys, and so on) are **not implemented yet**. LinkedIn is shown as **Coming Soon**.

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or Visual Studio 2022 (17.8+) with the .NET desktop workload

## Open in Visual Studio 2022

1. Open `WindowsWorkflowAutomator.sln`
2. Set `WindowsWorkflowAutomator` as the startup project
3. Press F5

## Build and run from the command line

```bash
dotnet build WindowsWorkflowAutomator.sln
dotnet run --project Source/WindowsWorkflowAutomator
```

## Solution layout

```text
WindowsWorkflowAutomator/
├── Documentation/          Architecture notes
├── Source/WindowsWorkflowAutomator/
│   ├── UI/                 Main window, navigation, placeholder pages
│   ├── Models/             Plain data objects
│   ├── Data/               EF Core DbContext
│   ├── Repositories/       Data access
│   ├── Services/           Application startup (no Form business logic)
│   ├── Automation/         Reserved
│   ├── FileOrganizer/      Reserved
│   ├── SocialMedia/        Reserved
│   ├── GitHub/             Reserved
│   ├── Scheduler/          Reserved
│   ├── Licensing/          Reserved
│   ├── Security/           Reserved
│   ├── Logging/            File logger
│   ├── Configuration/      Paths, settings, appsettings.json
│   └── Utilities/          Small helpers
├── Tests/                  xUnit tests
└── Assets/                 Icons and images (later)
```

Local runtime files (SQLite database and log files) are stored under:

`%LocalAppData%\WindowsWorkflowAutomator\`

## Architecture rules

- Keep business logic out of `Form` classes
- UI pages are `UserControl`s hosted in the main window
- External APIs are not faked; placeholders stay empty until a later phase

## License

Course project. Not a commercial product.
