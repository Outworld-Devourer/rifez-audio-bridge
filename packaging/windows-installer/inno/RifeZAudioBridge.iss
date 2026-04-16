#define MyAppName "RifeZ Audio Bridge"
#define MyAppVersion "0.1.0-demo"
#define MyAppPublisher "RifeZ"
#define MyAppExeName "RifeZAudioBridge.App.exe"
#define MyAppId "{{D8B4E8F7-1A6D-4A5A-9D21-6F08A6B90F41}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\RifeZ Audio Bridge
DefaultGroupName=RifeZ Audio Bridge
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=RifeZAudioBridge-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]


[Files]
; Windows tray app payload
Source: "..\..\release\windows\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Driver payload staged separately under the install folder
;Source: "..\..\release\windows\driver\*"; DestDir: "{app}\driver"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\RifeZ Audio Bridge"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\RifeZ Audio Bridge"; Filename: "{app}\{#MyAppExeName}"

[Registry]


[Run]
; Optional driver install step - currently assumes INF is present in staged driver payload.
; Replace the INF name below if your staged driver file uses a different name.

; Launch app after setup

[UninstallRun]
; Placeholder for future driver uninstall path if desired.
; Be careful: uninstalling by OEM INF published name is not stable until tracked explicitly.

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;