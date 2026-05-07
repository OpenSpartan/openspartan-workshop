# OpenSpartan Workshop 1.0.12 (`INFINITY-05062026`)

This release fixes localization rendering and adds proper architecture support so the app installs and runs natively on x64, ARM64, and x86 Windows.

- **Multi-architecture installers.** The release now publishes three bundles — `OpenSpartan.Workshop.Installer.Bundle-x64.exe`, `-ARM64.exe`, and `-x86.exe`. Each ships the matching .NET 10 Desktop Runtime and Windows App Runtime 1.8 for that architecture, so ARM64 users no longer fall back to slow x64 emulation and the "This Application Requires Windows App Runtime" error on ARM64 / x86 installs is resolved (issue [#53](https://github.com/OpenSpartan/openspartan-workshop/issues/53)). Pick the bundle matching your CPU; if you're unsure, run `echo %PROCESSOR_ARCHITECTURE%` in Command Prompt.
- **Percentage formatting.** Fixed percentage values displaying ~100x too large on locales such as English (South Africa), French, German, and others where `,` is the decimal separator (issue [#51](https://github.com/OpenSpartan/openspartan-workshop/issues/51)). Career accuracy, rank progress, and experience progress now render correctly.
- **Build metadata.** The User-Agent sent to Halo APIs and the build identifier shown on the Settings page now correctly track each release; the previous `BuildId` constant had been silently stale across multiple releases.

<!-- TODO: append additional 1.0.12 changes here as they land before tagging. -->

Refer to [**getting started guide**](https://openspartan.com/docs/workshop/guides/get-started/) to start using OpenSpartan Workshop.
