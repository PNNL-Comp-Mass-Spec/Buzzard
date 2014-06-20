; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!
                                    
; MyAppVerName will appear in the Uninstall Programs list
#define MyAppVerName "Buzzard_1.7.10"
#define MySource "F:\My Documents\Projects\BrianLaMarche\LCMS\Applications\Buzzard\BuzzardWPF"
#define MyLib    "F:\My Documents\Projects\BrianLaMarche\LCMS\lib"
#define MyAppName "Buzzard"
#define MyAppVis  "PNNL"
#define MyAppPublisher "Battelle"
#define MyAppExeName "BuzzardWPF.exe"  
#define MyDateTime GetDateTimeString('mm_dd_yyyy', "_","_");

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C617288A-CBA4-44C7-9899-153B4AC1F34F}}
AppName={#MyAppName}
AppVerName={#MyAppVerName}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=F:\My Documents\Projects\BrianLaMarche\LCMS\installers\Buzzard-{#MyDateTime}
OutputBaseFilename={#MyAppVerName}_{#MyAppVis}_{#MyDateTime}
SourceDir={#MySource}
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]        
Name: "{app}\x64"
Name: "{app}\x86"

[Files]
; Exe and supporting libraries
Source: "{#MySource}\bin\x86\Release\BuzzardWPF.exe";                         DestDir: "{app}";             Flags: ignoreversion
Source: "{#MySource}\bin\x86\Release\BuzzardWPF.exe.config";                  DestDir: "{app}";             Flags: ignoreversion
Source: "{#MySource}\bin\x86\Release\BuzzardLib.dll";                         DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\Finch.dll";                              DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\LcmsNetDmsTools.dll";                    DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\LcmsNetSDK.dll";                         DestDir: "{app}";             Flags: ignoreversion
Source: "{#MySource}\bin\x86\Release\RESTEasyHTTP.dll";                       DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\System.Data.SQLite.DLL";                 DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\System.Data.SQLite.xml";                 DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\System.Windows.Controls.Input.Toolkit.dll";    DestDir: "{app}";       Flags: ignoreversion 
Source: "{#MySource}\bin\x86\Release\System.Windows.Controls.Layout.Toolkit.dll";   DestDir: "{app}";       Flags: ignoreversion 
Source: "{#MySource}\bin\x86\Release\WPFToolkit.dll";                         DestDir: "{app}";             Flags: ignoreversion   
Source: "{#MySource}\bin\x86\Release\WPFToolkit.Extended.dll";                DestDir: "{app}";             Flags: ignoreversion
Source: "{#MySource}\bin\x86\Release\ZedGraph.DLL";                           DestDir: "{app}";             Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\x86\Sqlite.Interop.DLL";                 DestDir: "{app}\x86";         Flags: ignoreversion  
Source: "{#MySource}\bin\x86\Release\x64\Sqlite.Interop.DLL";                 DestDir: "{app}\x64";         Flags: ignoreversion  


;-----------------------------------------------------------------------------------------------------------------------------------------------

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\IconImage.ico"; WorkingDir: "{app}"; Tasks: desktopicon

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


