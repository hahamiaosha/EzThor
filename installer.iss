; ThorFlasher Inno Setup Installer Script
; Requires Inno Setup 6+ — https://jrsoftware.org/isinfo.php

#define MyAppName "ThorFlasher"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "QNAP"
#define MyAppExeName "ThorFlasher.exe"

[Setup]
AppId={{B7A2E3F1-5C4D-4E6F-8A9B-1D2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=publish
OutputBaseFilename=ThorFlasher_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Main executable (single-file self-contained)
Source: "publish\app\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Configuration
Source: "publish\app\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion

; Shell scripts
Source: "publish\app\Thor_Script\*"; DestDir: "{app}\Thor_Script"; Flags: ignoreversion recursesubdirs createallsubdirs

; Any remaining support files (PDBs, native libs, etc.)
Source: "publish\app\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "publish\app\*.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ThorFlasher"; Flags: nowait postinstall skipifsilent
