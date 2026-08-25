Release notes (draft)

Target branch: develop

Summary of changes in this local update:

- feat(licensing): enforce premium gating for automatic scheduler runs, GitHub Smart Auto-Sync UI, and workflow creation limits for Free tier
  - Scheduler: Start automatic scheduled execution only when a valid Premium license is present (ApplicationStartup)
  - GitHub: Prevent enabling Smart Auto Sync when license is Free (warning dialog and revert to Manual)
  - Workflows: Free tier limited to 3 workflows — creating more requires Premium (WorkflowService enforcement)
- tests: Added LicensingTests to validate LicenseRepository and LicenseService (in-memory SQLite)
- docs: PROJECT_STATUS.md updated to reflect real repo state; README.md updated with full project overview and demo checklist
- docs: Added DEMO_CHECKLIST.md for demo-run guidance

Notes for reviewer:
- These are local commits only; push not performed here.
- Tests pass locally (25/25). Please run `dotnet test` after pulling.
- Publishing: `dotnet publish Source/WindowsWorkflowAutomator -c Release -r win-x64 --self-contained false` produces release artifacts under publish\win-x64

Suggested PR title: "feat(licensing): local premium gating for scheduler & GitHub auto-sync; add license tests and docs"
Suggested PR description: include the summary above and explicitly note that automatic scheduled tasks are now gated by license tier and that Smart Auto Sync remains a UI-gated Premium feature (does not auto-push to GitHub).

Remaining work before final release:
- Implement AI captioning (AiAssisted) if required
- Add nicer License upgrade CTA in Settings (link/flow to obtain license)
- Dashboard: wire summary cards to live data

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>