; ============================================================
;  Admin Toolbox — Inno Setup 6 Script
;  Produces:  AdminToolbox-Setup.exe
;  Target OS: Windows 10 / 11 (x64)
; ============================================================

#define AppName      "Admin Toolbox"
#define AppVersion   "1.0.0"
#define AppPublisher "IT-BAER"
#define AppExeName   "AdminToolbox.exe"
#define AppGuid      "{{A7B3C2D1-4E5F-4A6B-8C9D-0E1F2A3B4C5D}"
#define SourceDir    "..\bin\Publish"

[Setup]
AppId={#AppGuid}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=
DefaultDirName={autopf}\IT-BAER\Admin Toolbox
DefaultGroupName=IT-BAER\Admin Toolbox
OutputDir=..\bin\Installer
OutputBaseFilename=AdminToolbox-Setup
SetupIconFile={#SourceDir}\Assets\AdminToolbox.ico
Compression=lzma2/ultra64
SolidCompression=yes
; Require elevation — needed to install RSAT and write to Program Files
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=
; Windows 10 1809+ (build 17763)
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Wizard style
WizardStyle=modern
DisableProgramGroupPage=yes
; Uninstaller
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CreateUninstallRegKey=yes

[Languages]
Name: "german";  MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
; Explicitly track the parent "IT-BAER" folder so Inno Setup removes it
; (if empty) when the application is uninstalled.
Name: "{autopf}\IT-BAER"
; Same for the Start Menu parent group
Name: "{commonprograms}\IT-BAER"

[Files]
; All publish output (EXE + WPF native DLLs) — wildcard so it works regardless
; of which optional DLLs the runtime decided to include.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion
; Assets folder (app icon — required by the notification-area tray icon)
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion
; RSAT install script
Source: "install-rsat.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\{#AppName}";  Filename: "{app}\{#AppExeName}"
; Desktop shortcut
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Tasks]
; RSAT feature installation checkboxes
Name: "rsat";                Description: "RSAT-Features installieren"; GroupDescription: "Zusätzliche Features:"; Flags: checkablealone
Name: "rsat\ad";             Description: "Active Directory (Users, Domains, Sites)";  Flags: checkedonce
Name: "rsat\dhcp";           Description: "DHCP Server Management";                     Flags: checkedonce
Name: "rsat\dns";            Description: "DNS Server Management";                      Flags: checkedonce
Name: "rsat\gpo";            Description: "Group Policy Management";                    Flags: checkedonce
Name: "rsat\print";          Description: "Print Management";                           Flags: checkedonce
Name: "rsat\cert";           Description: "Certificate Services (CA, Templates)";       Flags: checkedonce
Name: "rsat\filesvcs";       Description: "File Services (DFS, Shared Folders)";        Flags: checkedonce

[Run]
; Install selected RSAT features silently after files are in place
Filename: "powershell.exe"; \
  Parameters: "-NonInteractive -NoProfile -ExecutionPolicy Bypass -File ""{app}\install-rsat.ps1"" -Features ""{code:GetSelectedRsatFeatures}"""; \
  Flags: runhidden waituntilterminated; \
  StatusMsg: "Installiere ausgewählte RSAT-Features..."; \
  Description: "RSAT-Features installieren"; \
  Check: AnyRsatTaskSelected

; Offer to launch immediately after install
; shellexec is required because the app manifest requests requireAdministrator —
; ShellExecuteEx handles the elevation handoff correctly; CreateProcess does not.
Filename: "{app}\{#AppExeName}"; \
  Flags: nowait postinstall skipifsilent shellexec; \
  Description: "{#AppName} jetzt starten"

[UninstallRun]
; On uninstall, do NOT remove RSAT — they may be used by other tools.
; Nothing extra needed here.

[Code]
// -------------------------------------------------------
// RSAT task → feature-name mapping
// -------------------------------------------------------
function GetSelectedRsatFeatures(Param: String): String;
var
  Features: String;
begin
  Features := '';
  if WizardIsTaskSelected('rsat\ad')       then Features := Features + 'Rsat.ActiveDirectory.DS-LDS.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\dhcp')     then Features := Features + 'Rsat.DHCP.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\dns')      then Features := Features + 'Rsat.Dns.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\gpo')      then Features := Features + 'Rsat.GroupPolicy.Management.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\print')    then Features := Features + 'Rsat.PrintManagement.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\cert')     then Features := Features + 'Rsat.CertificateServices.Tools~~~~0.0.1.0,';
  if WizardIsTaskSelected('rsat\filesvcs') then Features := Features + 'Rsat.FileServices.Tools~~~~0.0.1.0,';
  // Remove trailing comma
  if Length(Features) > 0 then
    Features := Copy(Features, 1, Length(Features) - 1);
  Result := Features;
end;

function AnyRsatTaskSelected(): Boolean;
begin
  Result := WizardIsTaskSelected('rsat\ad') or WizardIsTaskSelected('rsat\dhcp') or
            WizardIsTaskSelected('rsat\dns') or WizardIsTaskSelected('rsat\gpo') or
            WizardIsTaskSelected('rsat\print') or WizardIsTaskSelected('rsat\cert') or
            WizardIsTaskSelected('rsat\filesvcs');
end;

// -------------------------------------------------------
// Check Windows version at startup — block on anything
// older than Windows 10 1809.
// -------------------------------------------------------
function InitializeSetup(): Boolean;
var
  v: TWindowsVersion;
begin
  GetWindowsVersionEx(v);
  if (v.Major < 10) or ((v.Major = 10) and (v.Build < 17763)) then
  begin
    MsgBox('Admin Toolbox requires Windows 10 (version 1809) or later.', mbError, MB_OK);
    Result := False;
  end else
    Result := True;
end;

// -------------------------------------------------------
// After uninstall completes, remove the parent publisher
// directories if they are empty (belt-and-suspenders — the
// [Dirs] entries handle new installs; this covers upgrades
// from older versions that had no [Dirs] entry).
// -------------------------------------------------------
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ParentDir: String;
  StartMenuParent: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Program Files parent
    ParentDir := ExpandConstant('{autopf}\IT-BAER');
    if DirExists(ParentDir) then
      RemoveDir(ParentDir);   // succeeds only when empty — safe to call unconditionally

    // Start Menu parent
    StartMenuParent := ExpandConstant('{commonprograms}\IT-BAER');
    if DirExists(StartMenuParent) then
      RemoveDir(StartMenuParent);
  end;
end;
