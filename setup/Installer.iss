; BlueMeter Inno Setup Installer Script
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!
; Non-commercial use only
;
; Build Instructions:
; 1. Install Inno Setup from https://jrsoftware.org/isdl.php
; 2. Open this file in Inno Setup
; 3. Click Compile to generate BlueMeterSetup.exe
; 4. Requires Release build: dotnet build -c Release

#include "CodeDependencies.iss"

#define MyAppName "BlueMeter"
#define MyAppVersion "1.3.6"
#define MyAppURL "https://github.com/caaatto/BlueMeter"
#define MyAppExeName "BlueMeter.WPF.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{347CB01F-1571-4A67-8B0E-48CF290102E2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
SetupIconFile=..\BlueMeter.WPF\Assets\Images\ApplicationIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run
; on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the
; install be done in "64-bit mode" on x64 or Windows 11 on Arm,
; meaning it should use the native 64-bit Program Files directory and
; the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE.txt
; Uncomment the following line to run in non administrative install mode (install for current user only).
;PrivilegesRequired=lowest
OutputBaseFilename=BlueMeterSetup
SolidCompression=yes
WizardStyle=modern
; Close applications during upgrade
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourcePath}\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: files; Name: "{app}\PlayerInfoCache.dat"
Type: files; Name: "{app}\*.log"
Type: dirifempty; Name: "{app}"

[Code]
var
  DeleteUserDataPage: TInputOptionWizardPage;

procedure InitializeUninstallProgressForm();
begin
  DeleteUserDataPage := CreateInputOptionPage(wpWelcome,
    'Remove User Data', 'Do you want to remove all BlueMeter user data?',
    'BlueMeter stores your settings, combat logs, and checklists in your user folder. ' +
    'Do you want to remove this data as well?' + #13#10 + #13#10 +
    'Location: ' + ExpandConstant('{localappdata}\BlueMeter'),
    False, False);
  DeleteUserDataPage.Add('Remove all user data (settings, combat logs, checklists)');
  DeleteUserDataPage.Add('Keep user data (you can manually delete it later)');
  DeleteUserDataPage.SelectedValueIndex := 1; // Default to keeping data
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataPath: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // If user chose to delete user data
    if DeleteUserDataPage.SelectedValueIndex = 0 then
    begin
      AppDataPath := ExpandConstant('{localappdata}\BlueMeter');
      if DirExists(AppDataPath) then
      begin
        if MsgBox('Are you sure you want to permanently delete all BlueMeter data?' + #13#10 + #13#10 +
                  'This will remove:' + #13#10 +
                  '- Your settings and configuration' + #13#10 +
                  '- All combat logs and statistics' + #13#10 +
                  '- Checklist data' + #13#10 + #13#10 +
                  'This action cannot be undone!',
                  mbConfirmation, MB_YESNO) = IDYES then
        begin
          DelTree(AppDataPath, True, True, True);
        end;
      end;
    end;
  end;
end;

[Code]
function InitializeSetup: Boolean;
begin
  // add the dependencies you need
  Dependency_AddDotNet80Desktop;  // WPF requires Desktop Runtime
  Dependency_AddNpcap;  // Packet capture driver (free edition, non-silent)

  Result := True;
end;
