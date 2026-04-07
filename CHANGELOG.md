# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2026-04-07

### Fixed
- Fixed a managed memory leak in `LoginWindow` where `PasswordBox.Password` allocated a string on the managed heap. Reverted back to explicit extraction since `wpf-ui` obscures `SecurePassword`.
- Replaced the 'X' Window close behavior so the app hides smoothly into the notification area (system tray) to prevent accidental exits. Explicit lock and close events securely wipe the credentials.

## [1.1.0] - 2025-07-23

### Changed

- Migrated UI framework from ModernWpfUI 0.9.6 (unmaintained) to [WPF UI (Lepo)](https://github.com/lepoco/wpfui) 4.2.0
- Windows now use `FluentWindow` with native title bar controls (minimize, maximize, close)
- Updated theme resources to WPF UI `ThemesDictionary` and `ControlsDictionary`

### Added

- Single-instance enforcement — launching the shortcut while the app is already running (even when minimized to tray) brings the existing window to the foreground instead of opening a new process
- Remember last used username across sessions (stored in `%LOCALAPPDATA%\AdminToolbox\`)
- Double-click protection on tool tiles — buttons are temporarily disabled for 2 seconds after click to prevent duplicate launches

## [1.0.0] - 2025-07-22

### Added

- WPF desktop dashboard with Fluent Design (WPF UI) for launching Windows admin tools
- Domain admin credential vault using SecureString with in-memory-only storage
- P/Invoke `CreateProcessWithLogonW` for launching MMC snap-ins under alternate credentials
- 10 RSAT tools: AD Users & Computers, AD Domains & Trusts, AD Sites & Services, DHCP, DNS, Group Policy Management, Print Management, Certificate Authority, Certification Authority Web Enrollment, File Server Resource Manager
- 11 built-in Windows admin tools: Computer Management, Disk Management, Event Viewer, Services, Task Scheduler, Performance Monitor, Device Manager, Local Security Policy, Windows Firewall, Hyper-V Manager, iSCSI Initiator
- Dynamic tool detection based on installed `.msc` snap-ins
- Refresh button to re-scan available tools at runtime
- System tray integration with lock and exit controls
- Smooth lerp-based scrolling for tool tile list
- Inno Setup 6 installer with selectable RSAT feature checkboxes (AD, DHCP, DNS, GPO, Print, Certificates, File Services)
- Silent installation support via `/VERYSILENT /TASKS=...` switches
- PowerShell RSAT capability installer script
- UAC elevation via application manifest (`requireAdministrator`)
- Build script for automated publish and installer generation

[1.1.0]: https://github.com/IT-BAER/admin-toolbox/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/IT-BAER/admin-toolbox/releases/tag/v1.0.0
[1.1.1]: https://github.com/IT-BAER/admin-toolbox/compare/v1.1.0...v1.1.1
