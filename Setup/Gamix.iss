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

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait

[Code]
var
  MaintenancePage: TInputOptionWizardPage;
  IsUpgrade: Boolean;

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
  UninstallString := GetUninstallString();
  if UninstallString <> '' then
  begin
    // /SILENT オプションを追加してサイレント実行
    UninstallString := RemoveQuotes(UninstallString);
    Result := Exec(UninstallString, '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end;
end;

// メンテナンスページの作成
procedure InitializeWizard();
begin
  if IsAppInstalled() then
  begin
    IsUpgrade := True;
    
    MaintenancePage := CreateInputOptionPage(wpWelcome,
      '{#MyAppName} は既にインストールされています',
      '実行する操作を選択してください',
      '以下のオプションから選択し、「次へ」をクリックしてください。',
      True, False);
    
    MaintenancePage.Add('修復 - アプリケーションを再インストールします');
    MaintenancePage.Add('アンインストール - アプリケーションを削除します');
    MaintenancePage.Values[0] := True; // デフォルトは修復
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
      if MsgBox('本当にアンインストールしますか?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DoUninstall();
        Result := False; // セットアップを終了
        WizardForm.Close();
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
