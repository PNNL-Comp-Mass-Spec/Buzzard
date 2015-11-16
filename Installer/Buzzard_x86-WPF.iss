; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!
                                    
; MyAppVerName will appear in the Uninstall Programs list
#define ReleaseVersion GetFileVersion('..\BuzzardWPF\bin\x86\Release\BuzzardWPF.exe')
;#define MyAppVerName "Buzzard_1.7.12.5"
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
OutputDir={#InstallerFolder}\Buzzard-{#MyDateTime}
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
Source: "bin\x86\Release\BuzzardWPF.exe";                         DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\BuzzardWPF.exe.config";                  DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\BuzzardLib.dll";                         DestDir: "{app}";             Flags: ignoreversion  
Source: "bin\x86\Release\PrismDMS.config";                        DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\LcmsNetDmsTools.dll";                    DestDir: "{app}";             Flags: ignoreversion  
Source: "bin\x86\Release\LcmsNetSDK.dll";                         DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\LcmsNetSQLiteTools.dll";                 DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\System.Data.SQLite.DLL";                 DestDir: "{app}";             Flags: ignoreversion  
Source: "bin\x86\Release\System.Data.SQLite.xml";                 DestDir: "{app}";             Flags: ignoreversion  
Source: "bin\x86\Release\System.Windows.Controls.Input.Toolkit.dll";    DestDir: "{app}";       Flags: ignoreversion 
Source: "bin\x86\Release\System.Windows.Controls.Layout.Toolkit.dll";   DestDir: "{app}";       Flags: ignoreversion 
Source: "bin\x86\Release\Ookii.Dialogs.Wpf.dll";                  DestDir: "{app}";             Flags: ignoreversion 
Source: "bin\x86\Release\WPFToolkit.dll";                         DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.AvalonDock.dll";               DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.AvalonDock.Themes.Aero.dll";   DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.AvalonDock.Themes.Metro.dll";  DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.AvalonDock.Themes.VS2010.dll"; DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.DataGrid.dll";                 DestDir: "{app}";             Flags: ignoreversion   
Source: "bin\x86\Release\Xceed.Wpf.Toolkit.dll";                  DestDir: "{app}";             Flags: ignoreversion
Source: "bin\x86\Release\x86\Sqlite.Interop.DLL";                 DestDir: "{app}\x86";         Flags: ignoreversion  
Source: "bin\x86\Release\x64\Sqlite.Interop.DLL";                 DestDir: "{app}\x64";         Flags: ignoreversion  
Source: "Resources\IconImage.ico";                                DestDir: "{app}";             Flags: ignoreversion  
Source: "..\RevisionHistory.txt";                                 DestDir: "{app}";             Flags: ignoreversion  

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
    NetFrameWorkInstalled : Boolean;
begin
	NetFrameWorkInstalled := RegKeyExists(HKLM,'SOFTWARE\Microsoft\Net Framework Setup\NDP\v4.0');
	if NetFrameWorkInstalled =true then
	begin
		Result := true;
	end;

	if NetFrameWorkInstalled =false then
	begin
		MsgBox('This setup requires the .NET Framework 4.0. Please install the .NET Framework and run this setup again.',
			mbInformation, MB_OK);
		Result:=false;
	end;
end;


