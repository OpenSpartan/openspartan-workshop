# OpenSpartan Workshop 1.0.11 (`HUNTER-05062026`)

This release focuses on stability, thread safety, and performance, alongside a number of internal refactors and dependency updates.

- Refactored the API layer to use updated client namespaces and methods, with improved error handling throughout.
- Improved UI responsiveness by moving brush and color creation off the synchronous path with lazy-initialized properties, batching UI updates, and reducing dispatcher calls.
- Parallelized data fetching and image downloads for faster bootstrap and refresh.
- Adopted nullable reference types, sealed/internal modifiers, and defensive null and file-existence checks across models, converters, and view models.
- Fixed an issue where matches were missing from the table due to local-time comparisons; timestamps are now normalized to UTC.
- Removed the WAM broker as the default sign-in path to address login regressions on some configurations.
- Updated SQLite, Microsoft Identity, Windows App SDK, and other package dependencies.
- Added PowerShell scripts (`perf-attach.ps1`, `perf-startup.ps1`) for Ultra-based profiling and Firefox Profiler integration.

Refer to [**getting started guide**](https://openspartan.com/docs/workshop/guides/get-started/) to start using OpenSpartan Workshop.
