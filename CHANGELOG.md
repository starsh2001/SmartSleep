# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.1] - 2025-09-28

### Improved
- **Performance Monitoring Accuracy**: Upgraded CPU and network monitoring to use Task Manager equivalent performance counters
  - CPU monitoring now uses `Processor Information\% Processor Utility` (Task Manager equivalent)
  - Network monitoring now uses `Network Interface\Bytes Total/sec` (Resource Monitor equivalent)
- **Dynamic Version Management**: Implemented centralized version management system
  - Application name and version now automatically reflect actual assembly version
  - Tooltip and settings window titles dynamically show "Smart Sleep 0.1.1"
- **Resource Management**: Added proper IDisposable pattern to all performance samplers
- **Code Quality**: Removed WMI dependencies in favor of more stable PerformanceCounter API

### Technical Details
- Replaced WMI-based monitoring with Windows Performance Counter API
- Added AppInfo utility class for centralized application metadata
- Improved error handling and fallback mechanisms for performance counters

## [0.1.0] - 2025-01-XX

### Added
- Initial release of SmartSleep application
- System idle monitoring with multiple detection methods:
  - Mouse/keyboard input activity detection with Windows hooks
  - CPU usage monitoring with configurable thresholds and smoothing
  - Network activity monitoring with configurable thresholds and smoothing
- Flexible scheduling system:
  - Always monitor mode
  - Daily time window mode
  - Weekly per-day schedule mode
  - Disabled mode
- Power management actions:
  - Sleep mode
  - Shutdown mode
- Advanced user interface:
  - System tray integration with real-time status tooltips
  - Comprehensive settings dialog with live status monitoring
  - Schedule-aware UI that disables indicators when monitoring is inactive
  - Immediate tooltip response with anti-flickering technology
- Multi-language support:
  - English and Korean localization
  - Language-aware status message prefixes
- Configuration management:
  - JSON-based configuration persistence
  - Centralized default values with comprehensive documentation
  - Windows startup integration
- Safety features:
  - Optional confirmation dialog with countdown
  - Sleep cooldown periods to prevent rapid triggering
  - Comprehensive logging of sleep/shutdown attempts

### Technical Features
- .NET 9.0 WPF application with Windows Forms integration
- MVVM architecture with dependency injection
- Real-time monitoring with configurable polling intervals
- Moving average smoothing for CPU and network metrics
- Windows API integration for precise idle time detection
- Last-change-wins priority system for status updates
- Self-contained deployment support

[Unreleased]: https://github.com/username/SmartSleep/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/username/SmartSleep/releases/tag/v0.1.0