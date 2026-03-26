# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-07-22

### Added

- WPF desktop dashboard with Fluent Design (ModernWpfUI) for launching Windows admin tools
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

[1.0.0]: https://github.com/IT-BAER/admin-toolbox/releases/tag/v1.0.0
