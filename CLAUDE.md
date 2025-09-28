# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SmartSleep is a Windows WPF tray application that monitors system idle conditions (mouse/keyboard input, CPU usage, network activity) and automatically triggers sleep mode when configured thresholds are met. The application uses .NET 9.0 with WPF and Windows Forms for system tray integration.

## Essential Commands

### Development
- `dotnet restore` - Install NuGet packages (first time setup)
- `dotnet build` - Build the application
- `dotnet run --project src/SmartSleep.App` - Run the application in development
- `dotnet format` - Format code according to style rules (install if missing)

### Publishing
- `build/publish-self-contained.ps1` - Create self-contained executable in `build/publish/`
- `build/publish-self-contained.bat` - Batch file wrapper for the PowerShell script

### Git Setup
- `git config core.hooksPath .githooks` - Enable commit message validation (one-time setup)

## Architecture & Key Components

### Application Structure
- **Entry Point**: `App.xaml.cs` - Single-instance WPF app with dependency injection setup
- **Services Layer**: Core business logic in `Services/` directory
  - `MonitoringService` - Orchestrates all monitoring activities and idle detection
  - `TrayIconService` - System tray integration and advanced tooltip management
  - `MouseMonitoringService` - Precise mouse position tracking for tooltip control
  - `ConfigurationService` - JSON config persistence to `config.json`
  - `SleepService` - Windows sleep/hibernate API integration
  - `AutoStartService` - Windows registry auto-start management
- **Models**: Data structures in `Models/` - `AppConfig`, `IdleSettings`, `ScheduleSettings`
- **Utilities**: Platform-specific monitoring in `Utilities/`
  - `InputActivityReader` - Win32 API for mouse/keyboard idle time with Windows hooks
  - `StatusDisplayHelper` - Unified status message and color logic for UI components
  - `LocalizationManager` - Multi-language support and resource management
  - `CpuUsageSampler` - Performance counter for CPU usage with moving average
  - `NetworkUsageSampler` - Network interface statistics with moving average
- **Views**: WPF UI in `Views/` - `SettingsWindow` (main config UI), `TrayTooltipWindow`
- **ViewModels**: MVVM pattern with `SettingsViewModel` and base `ViewModelBase`

### Configuration Management
Settings are persisted to `config.json` alongside the executable. The `AppConfig` class contains nested settings for idle detection (`IdleSettings`) and time-based scheduling (`ScheduleSettings`). Configuration changes are applied immediately to the monitoring service.

All default configuration values are centralized in `Configuration/DefaultValues.cs` with comprehensive XML documentation, organized into logical sections (Application Configuration, Idle Detection Settings, Schedule Settings, UI and Timing Constants, Native API Constants). This ensures consistent defaults across the application and easy maintenance.

### Monitoring Logic
The `MonitoringService` runs a continuous loop that:
1. Samples CPU usage, network usage, and input idle time
2. Applies moving averages to smooth sensor spikes
3. Determines idle state based on user-configured thresholds (requires all enabled conditions to be met)
4. Triggers sleep when idle duration exceeds configured timeout
5. Respects schedule restrictions and sleep cooldown periods

### Advanced UI Features
- **Real-time Status Display**: Live monitoring status with last-change-wins priority system
- **Smart Tooltips**: Advanced tray tooltips with immediate hide/show and anti-flickering
- **Schedule-aware UI**: Status indicators and colors automatically disable when monitoring schedule is inactive
- **Input Detection**: Windows hooks for immediate input activity detection (mouse/keyboard)
- **Localization**: Full Korean/English support with status message prefixes

## Development Guidelines

### Code Style
- Use file-scoped namespaces and implicit usings (enabled in project)
- Follow standard .NET naming: PascalCase for public members, camelCase for private fields with `_` prefix
- Async methods must have `Async` suffix
- Place each public type in its own file
- Use 4-space indentation

### Commit Guidelines
Follow Conventional Commits format: `type(scope?): description`
- Allowed types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`, `release`
- The commit hook validates this format automatically when configured

**CRITICAL**: Never commit or push changes without explicit user permission. This is essential guidance for future Claude Code interactions.

### Testing
No test project currently exists. When adding tests, use `dotnet new xunit -o src/SmartSleep.App.Tests` and mirror the production namespace structure. Test manually by running via `dotnet run` and verifying tray behavior, configuration persistence, and idle detection.

### Windows API Integration
The application uses P/Invoke for system integration via `Interop/NativeMethods.cs`. Handle all Win32 API failures gracefully and review privilege requirements for any new interop code.