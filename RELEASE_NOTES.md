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
- feat(social): add a built-in local caption assistant for AiAssisted mode; it generates an editable caption from a brief without an API key or network access
- fix(social): guard Instagram, TikTok, and YouTube adapter media collections to remove nullable build warnings

Notes for reviewer:
- These are local commits only; push not performed here.
- Release build passes with 0 warnings; tests pass locally (29/29). Please run `dotnet test` after pulling.
- Publishing completed: `publish\win-x64` contains the win-x64 framework-dependent release package.

Suggested PR title: "feat(licensing): local premium gating for scheduler & GitHub auto-sync; add license tests and docs"
Suggested PR description: include the summary above and explicitly note that automatic scheduled tasks are now gated by license tier and that Smart Auto Sync remains a UI-gated Premium feature (does not auto-push to GitHub).

Remaining work before final release:
- Add nicer License upgrade CTA in Settings (link/flow to obtain license)
- Run a full manual UI walkthrough with real demo credentials before distribution

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
