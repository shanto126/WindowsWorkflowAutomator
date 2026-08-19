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

## What is intentionally missing

Workflow execution, file watching, GitHub/Facebook clients, license validation, and premium gating. Those belong to later phases.
