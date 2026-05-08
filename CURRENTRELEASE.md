# OpenSpartan Workshop 1.0.12 (`INFINITY-05082026`)

This release ships the multi-architecture installer pipeline, fixes a localization bug that broke percentage rendering on comma-decimal locales, and lands a large startup and Matches-view performance pass.

### Installation
- **Multi-architecture installers.** The release publishes three bundles — `OpenSpartan.Workshop.Installer.Bundle-x64.exe`, `-ARM64.exe`, and `-x86.exe`. Each ships the matching .NET 10 Desktop Runtime and Windows App Runtime 1.8 for that architecture, so ARM64 users no longer fall back to slow x64 emulation and the "This Application Requires Windows App Runtime" error on ARM64 / x86 installs is resolved (issue [#53](https://github.com/OpenSpartan/openspartan-workshop/issues/53)). Pick the bundle matching your CPU; if you're unsure, run `echo %PROCESSOR_ARCHITECTURE%` in Command Prompt.

### Bug fixes
- **Percentage formatting.** Fixed percentage values displaying ~100x too large on locales such as English (South Africa), French, German, and others where `,` is the decimal separator (issue [#51](https://github.com/OpenSpartan/openspartan-workshop/issues/51)). Career accuracy, rank progress, and experience progress now render correctly.
- **Operation Infinite calendar.** The calendar now correctly marks every day of an open-ended operation, and operation backgrounds are no longer clobbered by overlapping event imagery.
- **Asset path leak.** Progression assets could occasionally land in the root of the `C:\` or `D:\` drive when the asset path began with a leading slash; fixed.
- **Startup crash.** Resolved a `StackOverflowException` in the dispatcher helper that could trigger on app launch.
- **Window close crash.** The dispatcher helper is now null-safe so closing the window while an in-flight UI continuation is pending no longer raises `NullReferenceException`.
- **Matches view.** Stopped double-loading the first page on view open; the view now also auto-fills the visible rows on small windows instead of waiting for the user to scroll.

### Performance
- **Faster perceived startup.** The splash screen is dismissed as soon as MSAL completes rather than waiting for the full bootstrap; the Home view's below-the-fold sections defer their work, and the database bootstrap is offloaded off the UI thread.
- **Faster Matches view.** A new `StartTime` expression index, lazy deserialization of the `Teams` and `ParticipationInfo` columns, and removal of an unused `Teams` column fetch significantly reduce time-to-first-row.
- **SQLite tuning.** WAL journaling, query caching, batched availability lookups, and grouped asset upserts cut redundant round-trips on the bootstrap and Matches paths.
- **Image converters.** `File.Exists` results and resolved `ImageSource` instances are now cached, eliminating redundant disk hits on every binding evaluation.
- **Battle pass loading.** Battle pass population now runs in parallel.

### Internal
- **Build metadata.** The User-Agent sent to Halo APIs and the build identifier shown on the Settings page now correctly track each release; the previous `BuildId` constant had been silently stale across multiple releases.
- **Defensive hardening.** DataHandler ordinal-mismatch bugs fixed; broad exception catches narrowed where appropriate.
- **Clean build.** Resolved 664 build warnings; the project now builds with zero warnings.

Refer to [**getting started guide**](https://openspartan.com/docs/workshop/guides/get-started/) to start using OpenSpartan Workshop.
