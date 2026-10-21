; Inno Setup script for the one-step CyberSecOS installer.
; CI fills dist\CyberSecOS with the desktop app (CyberSecOS.exe) and the bundled
; API in server\, then runs:  iscc /DAppVersion=0.1.0 installer\CyberSecOS.iss
; It installs per user (no administrator prompt) into
; %LOCALAPPDATA%\Programs\CyberSecOS. Learner data lives in
; %LOCALAPPDATA%\CyberSecOS\data and is kept on uninstall.

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#define SourceDir "..\dist\CyberSecOS"

[Setup]
AppId={{6F3D0C2A-9B47-4E8E-A1C5-3B2E7D9F4A10}
AppName=CyberSecOS
AppVersion={#AppVersion}
AppPublisher=CyberSecOS
DefaultDirName={autopf}\CyberSecOS
DefaultGroupName=CyberSecOS
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=CyberSecOS-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\CyberSecOS.exe
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
; Clear the previous server build so an upgrade never mixes old and new files.
Type: filesandordirs; Name: "{app}\server"

[Icons]
Name: "{group}\CyberSecOS"; Filename: "{app}\CyberSecOS.exe"
Name: "{group}\Uninstall CyberSecOS"; Filename: "{uninstallexe}"
Name: "{autodesktop}\CyberSecOS"; Filename: "{app}\CyberSecOS.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CyberSecOS.exe"; Description: "{cm:LaunchProgram,CyberSecOS}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop a server left running so its files can be removed.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM cybersecos-server.exe"; Flags: runhidden; RunOnceId: "StopServer"
