# Architecture (Phase 1)

Windows Workflow Automator is a WinForms desktop app. The UI is a shell: a sidebar, a header, and a content host. Each module is a `UserControl` resolved from the DI container.

## Layers

| Layer | Responsibility |
| --- | --- |
| `UI` | Forms, pages, navigation only |
| `Services` | Application startup and future use-cases |
| `Repositories` | Database access |
| `Data` | EF Core + SQLite |
| `Configuration` | Paths and user settings |
| `Logging` | File-based logger |
| `Models` | POCOs, no UI types |

## Why WinForms + Generic Host?

`Host.CreateApplicationBuilder()` gives configuration, DI, and lifetimes without a custom container. Forms request services through constructors. `DbContext` is scoped; the UI form is a singleton for the process lifetime.

## File Organizer (Phase 3)

Rules live in SQLite (`FileOrganizationRules`). `IFileRuleService` and `IFileOrganizerService` own matching, move/copy/rename, duplicates, and locked files. `IDownloadFolderMonitor` uses `FileSystemWatcher` and waits until a file can be opened exclusively. The File Organizer page is UI only.

## What is still intentionally missing

Workflow execution, GitHub/Facebook clients, the scheduler, license validation, and premium gating. Those belong to later phases.
