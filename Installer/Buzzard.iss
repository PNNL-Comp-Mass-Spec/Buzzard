; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

; MyAppVerName will appear in the Uninstall Programs list
#define ReleaseVersion GetFileVersion('..\BuzzardWPF\bin\Release\BuzzardWPF.exe')
#define MyAppVerName "Buzzard_" + ReleaseVersion
#define MySource "..\BuzzardWPF"
#define MyAppName "Buzzard"
#define MyAppVis  "PNNL"
#define MyAppPublisher "PNNL"
#define MyAppExeName "BuzzardWPF.exe"
#define MyDateTime GetDateTimeString('mm_dd_yyyy', "_","_");
#define InstallerFolder "..\Installer\Output"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C617288A-CBA4-44C7-9899-153B4AC1F34F}}
AppName={#MyAppName}
;AppVerName={#MyAppVerName}
AppVerName={#MyAppName}_{#ReleaseVersion}
AppVersion={#ReleaseVersion}
VersionInfoVersion={#ReleaseVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#InstallerFolder}
OutputBaseFilename={#MyAppVerName}_{#MyAppVis}_{#MyDateTime}
SourceDir={#MySource}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
Name: "{app}\x64"
Name: "{app}\x86"

[Files]
; Exe and supporting libraries
Source: "bin\Release\BuzzardWPF.exe";                                 DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\BuzzardWPF.exe.config";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\LcmsNetData.dll";                                DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\LcmsNetDmsTools.dll";                            DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\LcmsNetSQLiteTools.dll";                         DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Ookii.Dialogs.Wpf.dll";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PrismDMS.config";                                DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PRISMWin.dll";                                   DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\ReactiveUI*.dll";                                DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Splat.dll";                                      DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Data.SQLite.dll";                         DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Reactive.dll";                            DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.ValueTuple.dll";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Windows.Controls.Input.Toolkit.dll";      DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Windows.Controls.Layout.Toolkit.dll";     DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\WpfExtras.dll";                                  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\WPFToolkit.dll";                                 DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Xceed.Wpf.Toolkit.dll";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\x86\Sqlite.Interop.DLL";                         DestDir: "{app}\x86"; Flags: ignoreversion
Source: "bin\Release\x64\Sqlite.Interop.DLL";                         DestDir: "{app}\x64"; Flags: ignoreversion
Source: "Resources\IconImage.ico";                                    DestDir: "{app}";     Flags: ignoreversion
Source: "..\RevisionHistory.txt";                                     DestDir: "{app}";     Flags: ignoreversion

;-----------------------------------------------------------------------------------------------------------------------------------------------

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\IconImage.ico"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
;Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
    key: string;
    NetFrameWorkInstalled : Boolean;
    release, minVersion: cardinal;
begin
    // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
    // http://kynosarges.org/DotNetVersion.html
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full';
    minVersion := 461808;
    Result := false;
    NetFrameWorkInstalled := RegKeyExists(HKLM, key);
    if NetFrameWorkInstalled =true then
    begin
        Result := RegQueryDWordValue(HKLM, key, 'Release', release);
        Result := Result and (release >= minVersion);
    end;

    if Result =false then
    begin
        MsgBox('This setup requires the .NET Framework version 4.7.2. Please install the .NET Framework and run this setup again.',
            mbInformation, MB_OK);
    end;
end;


