[Setup]
AppName=Screentation
AppVersion=2.0.3
AppPublisher=Almanex
AppPublisherURL=https://github.com/Almanex/Screentation-V2
DefaultDirName={localappdata}\Programs\Screentation
DefaultGroupName=Screentation
OutputDir=D:\Develop\Screentation-V2
OutputBaseFilename=Screentation-v2.0.3-setup-win-x64
SetupIconFile=D:\Develop\Screentation-V2\Screentation\Assets\AppIcon.ico
Compression=lzma2/max
SolidCompression=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\Screentation.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Files]
Source: "D:\Develop\Screentation-V2\Screentation\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\Screentation.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Develop\Screentation-V2\Screentation\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Screentation"; Filename: "{app}\Screentation.exe"
Name: "{autodesktop}\Screentation"; Filename: "{app}\Screentation.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\Screentation.exe"; Description: "{cm:LaunchProgram,Screentation}"; Flags: nowait postinstall skipifsilent
