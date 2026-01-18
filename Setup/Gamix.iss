; Gamix Installer Script for Inno Setup

#define MyAppName "Gamix"
#define MyAppVersion "0.0.1"
#define MyAppPublisher "Gamix Team"
#define MyAppExeName "Gamix.UI.exe"
#define MyAppId "{7B2F6E1C-6A5D-4E92-B7A1-4F9E82C3D04C}"

[Setup]
AppId={{7B2F6E1C-6A5D-4E92-B7A1-4F9E82C3D04C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\Output
OutputBaseFilename=Gamix-{#MyAppVersion}-win-x64
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
ShowLanguageDialog=auto
UsePreviousLanguage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent; Check: ShouldShowLaunchCheckbox

[CustomMessages]
english.MaintenanceTitle={#MyAppName} is already installed.
english.MaintenanceSubTitle=Select the operation you want to perform.
english.MaintenanceInstruction=Select one of the following options and click Next.
english.MaintenanceUpgrade=Upgrade - Upgrade to version {#MyAppVersion}
english.MaintenanceDowngrade=Downgrade - Reinstall older version {#MyAppVersion}
english.MaintenanceRepair=Repair - Reinstall the application
english.MaintenanceUninstall=Uninstall - Remove the application
english.MaintenanceConfirmUninstall=Are you sure you want to uninstall?

japanese.MaintenanceTitle={#MyAppName} は既にインストールされています
japanese.MaintenanceSubTitle=実行する操作を選択してください
japanese.MaintenanceInstruction=以下のオプションから選択し、「次へ」をクリックしてください。
japanese.MaintenanceUpgrade=アップグレード - バージョン {#MyAppVersion} へ更新します
japanese.MaintenanceDowngrade=ダウングレード - 古いバージョン {#MyAppVersion} をインストールします
japanese.MaintenanceRepair=修復 - アプリケーションを再インストールします
japanese.MaintenanceUninstall=アンインストール - アプリケーションを削除します
japanese.MaintenanceConfirmUninstall=本当にアンインストールしますか?

[Code]
// Windows API: プロセスを即座に終了
procedure ExitProcess(uExitCode: UINT);
  external 'ExitProcess@kernel32.dll stdcall';

var
  MaintenancePage: TInputOptionWizardPage;
  IsUpgrade: Boolean;

// [Run]セクションのチェックボックス表示制御
function ShouldShowLaunchCheckbox: Boolean;
begin
  // 常に自動起動するため、チェックボックスは表示しない（[Run]エントリをスキップ）
  Result := False;
end;

// インストールプロセスのステップ変更イベント
procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  // インストール完了直後（完了画面の前）
  if CurStep = ssPostInstall then
  begin
    // アプリケーションを自動起動
    Exec(ExpandConstant('{app}\{#MyAppExeName}'), '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;

// インストール済みバージョンを取得
function GetInstalledVersion(): String;
var
  UninstallKey: String;
  DisplayVersion: String;
begin
  Result := '';
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1';
  
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, UninstallKey, 'DisplayVersion', DisplayVersion) then
    Result := DisplayVersion
  else if RegQueryStringValue(HKEY_CURRENT_USER, UninstallKey, 'DisplayVersion', DisplayVersion) then
    Result := DisplayVersion;
end;

// バージョン比較関数
// 戻り値: 1 (V1 > V2), 0 (V1 = V2), -1 (V1 < V2)
function CompareVersion(V1, V2: String): Integer;
var
  P1, P2: Integer;
  N1, N2: Integer;
  S1, S2: String;
begin
  Result := 0;
  S1 := V1;
  S2 := V2;
  
  while (Result = 0) and ((S1 <> '') or (S2 <> '')) do
  begin
    P1 := Pos('.', S1);
    if P1 > 0 then
    begin
      N1 := StrToIntDef(Copy(S1, 1, P1 - 1), 0);
      Delete(S1, 1, P1);
    end
    else
    begin
      if S1 <> '' then N1 := StrToIntDef(S1, 0) else N1 := 0;
      S1 := '';
    end;
    
    P2 := Pos('.', S2);
    if P2 > 0 then
    begin
      N2 := StrToIntDef(Copy(S2, 1, P2 - 1), 0);
      Delete(S2, 1, P2);
    end
    else
    begin
      if S2 <> '' then N2 := StrToIntDef(S2, 0) else N2 := 0;
      S2 := '';
    end;
    
    if N1 > N2 then Result := 1
    else if N1 < N2 then Result := -1;
  end;
end;

// 既存インストールを検出
function IsAppInstalled(): Boolean;
var
  UninstallKey: String;
begin
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1';
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, UninstallKey) or RegKeyExists(HKEY_CURRENT_USER, UninstallKey);
  
  // 移行期間のため、以前の間違ったキー名 (}} ) もチェック
  if not Result then
  begin
    UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}}_is1';
    Result := RegKeyExists(HKEY_LOCAL_MACHINE, UninstallKey) or RegKeyExists(HKEY_CURRENT_USER, UninstallKey);
  end;
end;

// アンインストーラーのパスを取得
function GetUninstallString(): String;
var
  UninstallKey: String;
  UninstallString: String;
begin
  Result := '';
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1';
  
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, UninstallKey, 'UninstallString', UninstallString) then
    Result := UninstallString
  else if RegQueryStringValue(HKEY_CURRENT_USER, UninstallKey, 'UninstallString', UninstallString) then
    Result := UninstallString;
    
  // 以前の間違ったキー名もチェック
  if Result = '' then
  begin
    UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}}_is1';
    if RegQueryStringValue(HKEY_LOCAL_MACHINE, UninstallKey, 'UninstallString', UninstallString) then
      Result := UninstallString
    else if RegQueryStringValue(HKEY_CURRENT_USER, UninstallKey, 'UninstallString', UninstallString) then
      Result := UninstallString;
  end;
end;

// アンインストールを実行
function DoUninstall(): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := False;
  
  // 実行中のアプリケーションを強制終了
  Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  UninstallString := GetUninstallString();
  if UninstallString <> '' then
  begin
    // /SILENT オプションを追加してサイレント実行（待機せず即座にセットアップを終了）
    UninstallString := RemoveQuotes(UninstallString);
    Result := Exec(UninstallString, '/SILENT', '', SW_HIDE, ewNoWait, ResultCode);
  end;
end;

// メンテナンスページの作成
procedure InitializeWizard();
var
  InstalledVersion: String;
  CompareResult: Integer;
begin
  if IsAppInstalled() then
  begin
    IsUpgrade := True;
    InstalledVersion := GetInstalledVersion();
    
    // バージョン比較: 1(Newer), 0(Same), -1(Older)
    if InstalledVersion <> '' then
      CompareResult := CompareVersion('{#MyAppVersion}', InstalledVersion)
    else
      CompareResult := 1; // バージョン取得失敗時はとりあえずアップグレード扱い

    MaintenancePage := CreateInputOptionPage(wpWelcome,
      CustomMessage('MaintenanceTitle'),
      CustomMessage('MaintenanceSubTitle'),
      CustomMessage('MaintenanceInstruction'),
      True, False);
    
    if CompareResult > 0 then
    begin
        // アップグレード
        MaintenancePage.Add(CustomMessage('MaintenanceUpgrade'));
    end
    else if CompareResult < 0 then
    begin
        // ダウングレード
        MaintenancePage.Add(CustomMessage('MaintenanceDowngrade'));
    end
    else
    begin
        // 修復 (バージョン同じ)
        MaintenancePage.Add(CustomMessage('MaintenanceRepair'));
    end;

    MaintenancePage.Add(CustomMessage('MaintenanceUninstall'));
    MaintenancePage.Values[0] := True; // デフォルトはアップグレード/修復/ダウングレード
  end
  else
  begin
    IsUpgrade := False;
  end;
end;

// 次へボタンのクリック処理
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  
  if IsUpgrade and (CurPageID = MaintenancePage.ID) then
  begin
    // アンインストールが選択された場合
    if MaintenancePage.Values[1] then
    begin
      if MsgBox(CustomMessage('MaintenanceConfirmUninstall'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        DoUninstall();
        // 確認ダイアログをスキップして即座にプロセス終了
        ExitProcess(0);
      end
      else
      begin
        Result := False; // キャンセル
      end;
    end;
    // 修復が選択された場合は通常通り続行
  end;
end;

// ウィザードページのスキップ制御
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  // 既にインストールされている場合、ディレクトリ選択ページをスキップ
  if IsUpgrade and (PageID = wpSelectDir) then
    Result := True;
end;

// アンインストール時にNotifyIconSettings（通知バー設定）のレジストリを削除
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  KeyPath: String;
  SubKeyNames: TArrayOfString;
  i: Integer;
  ExePath: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    KeyPath := 'Control Panel\NotifyIconSettings';
    
    if RegGetSubkeyNames(HKEY_CURRENT_USER, KeyPath, SubKeyNames) then
    begin
      for i := 0 to GetArrayLength(SubKeyNames) - 1 do
      begin
        if RegQueryStringValue(HKEY_CURRENT_USER, KeyPath + '\' + SubKeyNames[i], 'ExecutablePath', ExePath) then
        begin
          if Pos('Gamix.UI.exe', ExePath) > 0 then
          begin
            RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, KeyPath + '\' + SubKeyNames[i]);
          end;
        end;
      end;
    end;
  end;
end;
