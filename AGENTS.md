# Repository Guidelines

## Project Structure & Module Organization
SmartSleep ships as a single WPF tray application under `src/SmartSleep.App`. Feature code is separated by responsibility: `ViewModels/` handles MVVM bindings, `Views/` contains XAML windows, `Services/` integrates with Windows APIs, and `Utilities/` captures shared helpers. Idle tracking shims live in `Interop/`, while data conversions belong in `Converters/`. Runtime assets such as icons and localization files sit in `resources/`. Generated folders (`bin/`, `obj/`, `build/`) are disposable; exclude them from commits.

## Build, Test, and Development Commands
`dotnet restore` pulls NuGet dependencies the first time you sync. `dotnet build` compiles the tray app and validates the project file. Use `dotnet run --project src/SmartSleep.App` to launch the UI locally. Run `dotnet format` before pushing to enforce styling. `dotnet tool restore` rehydrates local tools if the manifest changes.

## Coding Style & Naming Conventions
Follow .NET defaults: four-space indentation, file-scoped namespaces, and PascalCase for types, public members, and XAML names. Prefer camelCase for locals and `_camelCase` for private readonly fields. Append `Async` to asynchronous methods, prefix interfaces with `I`, and place one public type per file. Keep XAML names descriptive (for example, `IdleSchedulePicker`).

## Testing Guidelines
An automated test project is not committed yet. When adding coverage, mirror the production namespace in `src/SmartSleep.App.Tests` (xUnit is recommended) and name tests `MethodUnderTest_State_ExpectedResult`. Until then, document manual checks: launch via `dotnet run`, toggle idle thresholds, and verify tray notifications and persisted config updates.

## Commit & Pull Request Guidelines
Adhere to Conventional Commit syntax: `type(scope?): summary` capped at 72 characters. Common types include `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `release`, and `revert`. Example: `feat(settings): add idle reset logic`. Enable local hooks once per clone with `git config core.hooksPath .githooks`; the `commit-msg` hook runs via PowerShell (or PowerShell 7) and blocks invalid summaries when available, so enforce the pattern manually if it is skipped. Keep commits focused, rebase onto `master` before opening a PR, squash only when a reviewer asks, and provide context-rich descriptions with linked issues plus manual or automated test evidence. Include before/after screenshots for UI-affecting work and request review before merging.

## Security & Configuration Tips
The app writes runtime settings to `config.json` beside the executable. Never commit personal copies; supply sanitized examples under `resources/` when needed. Audit interop updates for elevated privilege calls, handle Win32 failures defensively, and ensure new services dispose unmanaged handles properly.
