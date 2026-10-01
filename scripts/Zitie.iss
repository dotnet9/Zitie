; Zitie Windows installer.
; Build from the repository root with Inno Setup 6 and pass /DAppVersion=x.y.z.

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

[Setup]
AppId={{33EFC376-2EFC-4C57-BD22-F276A6EB3AEA}
AppName=Zitie
AppVersion={#AppVersion}
AppPublisher=Dotnet9
AppPublisherURL=https://github.com/dotnet9/Zitie
AppSupportURL=https://github.com/dotnet9/Zitie/issues
DefaultDirName={autopf}\Zitie
DefaultGroupName=Zitie
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Zitie-v{#AppVersion}-win-x64-setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
ChangesAssociations=no
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=Zitie.Desktop.exe
UninstallDisplayIcon={app}\Zitie.Desktop.exe
WizardStyle=modern

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Zitie"; Filename: "{app}\Zitie.Desktop.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Zitie"; Filename: "{app}\Zitie.Desktop.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Zitie.Desktop.exe"; Description: "Launch Zitie"; Flags: nowait postinstall skipifsilent
