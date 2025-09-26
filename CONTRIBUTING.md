# Repository Guidelines

## Project Structure & Module Organization
Application code lives under `src/SmartSleep.App`, structured by responsibility: `ViewModels/` for MVVM bindings, `Views/` for XAML screens, `Services/` for platform integration, and `Utilities/` for shared helpers. Native interop shims are grouped in `Interop/`, while UI converters sit in `Converters/`. Assets such as icons and localization files belong in `resources/`. Build artifacts in `bin/` and `obj/` are disposable; keep them out of commits.

## Build, Test, and Development Commands
- `dotnet restore` resolves NuGet packages the first time you work in the repo.
- `dotnet build` compiles the WPF application and validates the project file.
- `dotnet run --project src/SmartSleep.App` launches the tray app for manual verification.
- `dotnet format` (install if missing) enforces C# style rules before you push.

## Coding Style & Naming Conventions
Use four-space indentation and file-scoped namespaces. Follow standard .NET casing: PascalCase for classes, records, and public members; camelCase for locals and private fields (prefix `_` for private readonly fields). Name asynchronous methods with the `Async` suffix and interfaces with an `I` prefix. Keep XAML element names descriptive (for example, `IdleSchedulePicker`). Place each public type in its own file.

## Testing Guidelines
No automated test project ships with this snapshot. When adding tests, prefer `dotnet new xunit -o src/SmartSleep.App.Tests` and mirror the production namespace tree. Name test methods using `MethodUnderTest_State_ExpectedResult`. Until coverage exists, exercise key workflows manually: launch via `dotnet run`, toggle idle conditions, and confirm tray notifications and config persistence.

## Commit & Pull Request Guidelines
Follow Conventional Commits so the history stays easy to scan. Use the format ``type(scope?): short summary`` with the summary kept under 72 characters. Allowed ``type`` tokens: ``feat``, ``fix``, ``docs``, ``style``, ``refactor``, ``perf``, ``test``, ``build``, ``ci``, ``chore``, ``revert``, ``release``. Example: ``feat(settings): add idle reset logic``.

Keep each commit focused, prefer rebasing onto the latest ``master`` before opening a PR, and squash only when a reviewer asks for it. Pull requests should describe the change, list manual or automated test evidence, and link related issues. Include before/after screenshots for UI or tray icon tweaks, and request review from a maintainer before merging.

### Commit hook
Activate the local checker once per clone so commits are validated automatically:

```powershell
git config core.hooksPath .githooks
```

The ``commit-msg`` hook runs on PowerShell (or PowerShell 7) and blocks commits that do not match the convention. If PowerShell is unavailable, the hook skips the check, so please enforce the pattern manually in that case.

## Security & Configuration Tips
The app writes runtime configuration to `config.json` beside the executable. Do not check personal configs into source control; instead, supply sanitized samples under `resources/` if needed. Review interop changes for privilege escalation risks and confirm all Windows API calls handle failure paths gracefully.
