; Inno Setup 6 — скрипт установщика AudioSwitch.
; Не запускать ISCC вручную: используйте build-installer.ps1 в корне репозитория —
; он публикует приложение и вызывает ISCC с /D-определениями MyAppVersion,
; SetupArch, SourceDir и OutputDir.

#define MyAppName "AudioSwitch"
#define MyAppExeName "AudioSwitch.exe"
#define MyAppPublisher "AudioSwitch"
#define MyAppVersionDefault "1.0.0"
#ifndef MyAppVersion
  #define MyAppVersion MyAppVersionDefault
#endif
#ifndef SourceDir
  #define SourceDir AddBackslash(SourcePath) + "..\publish\AudioSwitch"
#endif
#ifndef OutputDir
  #define OutputDir AddBackslash(SourcePath) + "..\dist"
#endif
#ifndef SetupArch
  #define SetupArch "x64"
#endif

[Setup]
; AppId не менять при существовании установок пользователей.
AppId={{A22B09E8-1B82-4590-9EC2-38E939C5C9E0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
DisableProgramGroupPage=auto
DirExistsWarning=no
OutputDir={#OutputDir}
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-{#SetupArch}-setup
SetupIconFile=..\AudioSwitch\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Трейная утилита ставится в профиль без UAC ({autopf} -> AppData\Local\Programs);
; через диалог/командную строку можно повысить до "для всех пользователей".
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
CloseApplications=yes
RestartApplications=no
LanguageDetectionMethod=uilanguage
; AppMutex не используем: мьютекс AudioSwitch.SingleInstance существует и у
; dev-сборок из bin\, а завершение нужного процесса делает точечный
; KillInstalledApp по пути из [Code].

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "DesktopIcon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: DesktopIcon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

[Code]
const
  SettingsSubDir = 'AudioSwitch';
  RunKeyPath = 'Software\Microsoft\Windows\CurrentVersion\Run';

var
  DeleteSettings: Boolean;

procedure KillInstalledApp;
var
  ResultCode: Integer;
  Q, Prefix, Cmd: String;
begin
  // Трейное приложение перехватает запросы закрытия окна, поэтому завершаем
  // явно. Только процессы, запущенные из папки установки {app}, — dev-сборки
  // из bin\... останутся нетронутыми.
  Q := Chr(39);
  Prefix := Lowercase(ExpandConstant('{app}\'));
  Cmd := '-NoProfile -Command "$prefix=' + Q + Prefix + Q +
    '; Get-Process AudioSwitch -ErrorAction SilentlyContinue |' +
    ' Where-Object { $_.Path -and $_.Path.ToLower().StartsWith($prefix) } |' +
    ' Stop-Process -Force"';
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Cmd, '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CleanAutostartEntry;
var
  Value: String;
begin
  // Приложение само управляет HKCU\...\Run (значение AudioSwitch).
  // Удаляем запись только если она указывает на удаляемую папку установки,
  // чтобы не сломать автозапуск другой копии (например, dev-сборки).
  if RegQueryStringValue(HKCU, RunKeyPath, '{#MyAppName}', Value) then
    if Pos(Lowercase(ExpandConstant('{app}')), Lowercase(Value)) > 0 then
      RegDeleteValue(HKCU, RunKeyPath, '{#MyAppName}');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  KillInstalledApp;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usAppMutexCheck:
      KillInstalledApp;
    usPostUninstall:
      begin
        CleanAutostartEntry;
        if not WizardSilent then
          DeleteSettings :=
            MsgBox('Удалить настройки AudioSwitch (папку %APPDATA%\' + SettingsSubDir + ') ?',
                   mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
      end;
    usDone:
      if DeleteSettings then
        DelTree(ExpandConstant('{userappdata}\' + SettingsSubDir), True, True, True);
  end;
end;
