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
This distribution lacks Git metadata, so adopt Conventional Commits for clarity (for example, `feat: add network idle smoothing`). Keep commits focused and rebased on the latest main branch. Pull requests must state purpose, list manual or automated test evidence, and link related issues. Include before-and-after screenshots when you update UI or tray icons. Request review from a maintainer before merging.

## Security & Configuration Tips
The app writes runtime configuration to `config.json` beside the executable. Do not check personal configs into source control; instead, supply sanitized samples under `resources/` if needed. Review interop changes for privilege escalation risks and confirm all Windows API calls handle failure paths gracefully.
