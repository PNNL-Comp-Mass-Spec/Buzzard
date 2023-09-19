; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

; MyAppVerName will appear in the Uninstall Programs list
;#define ReleaseVersion GetFileVersion('..\BuzzardWPF\bin\Release\BuzzardWPF.exe')  
#define Major 1  
#define Minor 1 
#define Revision 1 
#define Build 1
#define FullVersion ParseVersion('..\BuzzardWPF\bin\Release\BuzzardWPF.exe', Major, Minor, Revision, Build)
#define ReleaseVersion Str(Major) + "." + Str(Minor) + "." + Str(Revision)
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
; As AnyCPU, we can install as 32-bit or 64-bit, so allow installing on 32-bit Windows, but make sure it installs as 64-bit on 64-bit Windows
; We need a method to un-install the 32-bit version when updating to the 64-bit version (not that it matters, we compile as AnyCPU, so it's just changing where it installs) 
;ArchitecturesAllowed=x64 x86
;ArchitecturesInstallIn64BitMode=x64
AppName={#MyAppName}
;AppVerName={#MyAppVerName}
AppVerName={#MyAppName}_{#ReleaseVersion}
AppVersion={#ReleaseVersion}
VersionInfoVersion={#ReleaseVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#InstallerFolder}
OutputBaseFilename={#MyAppVerName}_{#MyAppVis}_{#MyDateTime}
SourceDir={#MySource}
Compression=lzma
SolidCompression=yes
DisableWelcomePage=no
WizardStyle=modern
; Don't install to all users, instead install to the current user only. May install to all users if running as administrator
PrivilegesRequired=lowest
; Allow overriding the install mode via dialog or commandline (hides dialog); this lets the Buzzard auto-update continue to use the all-users install if the current install is all-users
; Command-line flags: /ALLUSERS, /CURRENTUSER
PrivilegesRequiredOverridesAllowed=dialog

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
Source: "bin\Release\DotNetProjects.Input.Toolkit.dll";               DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\DotNetProjects.Wpf.Extended.Toolkit.dll";        DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\DynamicData.dll";                                DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Microsoft.Bcl.AsyncInterfaces.dll";              DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Microsoft.Bcl.HashCode.dll";                     DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Microsoft.Extensions.Logging.Abstractions.dll";  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Npgsql.dll";                                     DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Ookii.Dialogs.Wpf.dll";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PRISM.dll";                                      DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PRISMDatabaseUtils.dll";                         DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PrismDMS.json";                                  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\PRISMWin.dll";                                   DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\ReactiveUI*.dll";                                DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\Splat*.dll";                                     DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Buffers.dll";                             DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Collections.Immutable.dll";               DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Data.SQLite.dll";                         DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Diagnostics.DiagnosticSource.dll";        DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Memory.dll";                              DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Numerics.Vectors.dll";                    DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Reactive.dll";                            DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Runtime.CompilerServices.Unsafe.dll";     DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Text.Encodings.Web.dll";                  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Text.Json.dll";                           DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Threading.Channels.dll";                  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.Threading.Tasks.Extensions.dll";          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\System.ValueTuple.dll";                          DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\WpfExtras.dll";                                  DestDir: "{app}";     Flags: ignoreversion
Source: "bin\Release\x86\Sqlite.Interop.DLL";                         DestDir: "{app}\x86"; Flags: ignoreversion
Source: "bin\Release\x64\Sqlite.Interop.DLL";                         DestDir: "{app}\x64"; Flags: ignoreversion
Source: "Resources\IconImage.ico";                                    DestDir: "{app}";     Flags: ignoreversion
Source: "..\RevisionHistory.txt";                                     DestDir: "{app}";     Flags: ignoreversion

; Include the BuzzardWPF pdb file to see line numbers in exception stack traces
Source: "bin\Release\BuzzardWPF.pdb";                                 DestDir: "{app}";     Flags: ignoreversion

[InstallDelete]
;Delete old DLLs that were used for Buzzard 2.4.* and older
Type: files; Name: "LcmsNet*.dll"

;-----------------------------------------------------------------------------------------------------------------------------------------------

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\IconImage.ico"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Run {#MyAppName}"; Flags: nowait postinstall skipifsilent

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


