using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.Core.Models;
using Gamix.Core.Services;
using Gamix.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamix.UI.ViewModels
{
    /// <summary>
    /// メインウィンドウ用の ViewModel。
    /// オーディオセッション一覧とプリセット管理を提供します。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly Gamix.Core.Audio.IAudioService _audioService;
        private readonly Gamix.Core.Services.IPresetService _presetService;
        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        
        /// <summary>
        /// アクティブなオーディオセッションのコレクション。
        /// </summary>
        public ObservableCollection<AudioSessionViewModel> Sessions { get; } = [];

        /// <summary>
        /// 保存されたプリセットのコレクション。
        /// </summary>
        public ObservableCollection<Preset> Presets { get; } = [];

        /// <summary>
        /// 利用可能なテーマ名のコレクション。
        /// </summary>
        public ObservableCollection<string> AvailableThemes { get; } = [];

        /// <summary>
        /// 利用可能な出力デバイス（スピーカー）のコレクション。
        /// </summary>
        public ObservableCollection<AudioDevice> OutputDevices { get; } = [];

        /// <summary>
        /// 利用可能な入力デバイス（マイク）のコレクション。
        /// </summary>
        public ObservableCollection<AudioDevice> InputDevices { get; } = [];

        /// <summary>
        /// 現在選択されているテーマ名。
        /// </summary>
        public string CurrentTheme => _themeService.CurrentTheme;

        /// <summary>
        /// 新規プリセット名の入力値。
        /// </summary>
        [ObservableProperty]
        private string _newPresetName = string.Empty;

        /// <summary>
        /// 選択された出力デバイス。
        /// </summary>
        [ObservableProperty]
        private AudioDevice? _selectedOutputDevice;

        /// <summary>
        /// 選択された入力デバイス。
        /// </summary>
        [ObservableProperty]
        private AudioDevice? _selectedInputDevice;

        /// <summary>
        /// 出力デバイスのマスター音量（0-100）。
        /// </summary>
        [ObservableProperty]
        private float _masterVolume;
        
        /// <summary>
        /// 出力デバイスがミュート状態かどうか。
        /// </summary>
        [ObservableProperty]
        private bool _isOutputMuted;

        /// <summary>
        /// 入力デバイスのマスター音量。
        /// </summary>
        [ObservableProperty]
        private float _inputMasterVolume;

        /// <summary>
        /// 入力デバイスがミュート状態かどうか。
        /// </summary>
        [ObservableProperty]
        private bool _isInputMuted;

        /// <summary>
        /// MainViewModel のコンストラクタ。
        /// </summary>
        /// <param name="audioService">オーディオセッション取得用のサービス。</param>
        /// <param name="presetService">プリセット管理用のサービス。</param>
        /// <param name="settingsService">設定保存用のサービス。</param>
        /// <param name="presetService">プリセット管理用のサービス。</param>
        /// <param name="settingsService">設定保存用のサービス。</param>
        /// <param name="themeService">テーマ管理用のサービス。</param>
        public MainViewModel(Gamix.Core.Audio.IAudioService audioService, Gamix.Core.Services.IPresetService presetService, ISettingsService settingsService, IThemeService themeService)
        {
            _audioService = audioService;
            _presetService = presetService;
            _settingsService = settingsService;
            _themeService = themeService;
            InitializeAsync();
        }

        /// <summary>
        /// 非同期で初期化を行います。
        /// </summary>
        private async void InitializeAsync()
        {
            await LoadDevicesAsync();
            await LoadMasterVolumeAsync();
            await LoadSessionsAsync();
            LoadPresets();
            
            // 起動時に Favorite プリセットを自動適用
            await ApplyFavoritePresetAsync();

            LoadThemes();
        }

        private void LoadThemes()
        {
            AvailableThemes.Clear();
            foreach (var theme in _themeService.GetAvailableThemes())
            {
                AvailableThemes.Add(theme);
            }
        }

        /// <summary>
        /// Favorite プリセットを自動適用します。
        /// </summary>
        private async Task ApplyFavoritePresetAsync()
        {
            if (SelectedOutputDevice == null) return;
            
            var favorite = await _presetService.GetFavoritePresetAsync(SelectedOutputDevice.Id);
            if (favorite != null)
            {
                await ApplyPreset(favorite);
            }
        }

        /// <summary>
        /// デバイス一覧を読み込みます。
        /// </summary>
        private async Task LoadDevicesAsync()
        {
            // 出力デバイスの読み込み
            var outputDevices = await _audioService.GetAudioDevicesAsync(true);
            OutputDevices.Clear();
            foreach (var device in outputDevices) OutputDevices.Add(device);

            // 入力デバイスの読み込み
            var inputDevices = await _audioService.GetAudioDevicesAsync(false);
            InputDevices.Clear();
            foreach (var device in inputDevices) InputDevices.Add(device);

            // 保存された設定を復元
            var savedOutputId = await _settingsService.GetSelectedOutputDeviceIdAsync();
            var savedInputId = await _settingsService.GetSelectedInputDeviceIdAsync();

            SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.Id == savedOutputId) 
                ?? OutputDevices.FirstOrDefault(d => d.IsDefault);
            SelectedInputDevice = InputDevices.FirstOrDefault(d => d.Id == savedInputId) 
                ?? InputDevices.FirstOrDefault(d => d.IsDefault);
        }

        /// <summary>
        /// 出力デバイスが変更されたときの処理。
        /// </summary>
        partial void OnSelectedOutputDeviceChanged(AudioDevice? value)
        {
            if (value != null)
            {
                // Windows のデフォルト出力デバイスも切り替え
                Gamix.Core.Audio.DefaultAudioDeviceSwitcher.SetDefaultDevice(value.Id);
                
                _ = _settingsService.SetSelectedOutputDeviceIdAsync(value.Id);
                _ = LoadSessionsAsync();
                _ = LoadMasterVolumeAsync();
                LoadPresets(); // デバイス変更時にプリセット一覧も更新
            }
        }

        /// <summary>
        /// 入力デバイスが変更されたときの処理。
        /// </summary>
        partial void OnSelectedInputDeviceChanged(AudioDevice? value)
        {
            if (value != null)
            {
                // Windows のデフォルト入力デバイスも切り替え
                Gamix.Core.Audio.DefaultAudioDeviceSwitcher.SetDefaultDevice(value.Id);

                _ = _settingsService.SetSelectedInputDeviceIdAsync(value.Id);
                _ = LoadInputMasterVolumeAsync();
            }
        }

        /// <summary>
        /// マスター音量が変更されたときの処理（0-100 スケール）。
        /// </summary>
        partial void OnMasterVolumeChanged(float value)
        {
            if (SelectedOutputDevice != null)
            {
                _audioService.SetDeviceMasterVolume(SelectedOutputDevice.Id, value / 100f);
            }
        }

        /// <summary>
        /// 入力デバイスのマスター音量が変更されたときの処理。
        /// </summary>
        partial void OnInputMasterVolumeChanged(float value)
        {
            if (SelectedInputDevice != null)
            {
                _audioService.SetDeviceMasterVolume(SelectedInputDevice.Id, value);
            }
        }

        /// <summary>
        /// 出力デバイスのマスター音量を読み込みます。
        /// </summary>
        private async Task LoadMasterVolumeAsync()
        {
            if (SelectedOutputDevice != null)
            {
                var (vol, isMuted) = await _audioService.GetDeviceMasterVolumeAsync(SelectedOutputDevice.Id);
                MasterVolume = vol * 100f;
                IsOutputMuted = isMuted;
            }
        }

        /// <summary>
        /// 入力デバイスのマスター音量を読み込みます。
        /// </summary>
        private async Task LoadInputMasterVolumeAsync()
        {
            if (SelectedInputDevice != null)
            {
                var (vol, isMuted) = await _audioService.GetDeviceMasterVolumeAsync(SelectedInputDevice.Id);
                InputMasterVolume = vol;
                IsInputMuted = isMuted;
            }
        }

        /// <summary>
        /// オーディオセッションを読み込みます。
        /// </summary>
        private async Task LoadSessionsAsync()
        {
            var deviceId = SelectedOutputDevice?.Id;
            var sessions = await _audioService.GetActiveSessionsAsync(deviceId);
            Sessions.Clear();
            foreach (var s in sessions)
            {
                // マスターボリュームは上部に専用UIがあるため、セッションリストからは除外
                if (s.IsMaster) continue;
                Sessions.Add(new AudioSessionViewModel(s, _audioService));
            }
        }

        /// <summary>
        /// プリセットを読み込みます。
        /// 選択中の出力デバイスに関連するプリセットのみを表示します。
        /// </summary>
        private async void LoadPresets()
        {
            var allPresets = await _presetService.LoadPresetsAsync();
            var deviceId = SelectedOutputDevice?.Id;
            
            Presets.Clear();
            foreach (var p in allPresets)
            {
                // 現在のデバイス用のプリセットのみ表示
                if (p.DeviceId == deviceId)
                {
                    Presets.Add(p);
                }
            }
        }

        /// <summary>
        /// 現在の音量状態をプリセットとして保存します。
        /// </summary>
        [RelayCommand]
        private async Task SavePreset()
        {
            if (string.IsNullOrWhiteSpace(NewPresetName) || SelectedOutputDevice == null) return;
            var currentModels = Sessions.Select(s => new AudioSession 
            { 
                Id = s.Id, 
                ProcessName = s.ProcessName, 
                Volume = s.Volume, 
                IsMuted = s.IsMuted 
            }).ToList();
            
            await _presetService.SavePresetAsync(
                NewPresetName, 
                SelectedOutputDevice.Id, 
                MasterVolume / 100f, // 0-100 を 0-1 に変換
                false, // 現在はミュート状態は未サポート
                currentModels);
            NewPresetName = string.Empty;
            LoadPresets();
        }

        /// <summary>
        /// 指定したプリセットを適用します。
        /// </summary>
        /// <param name="preset">適用するプリセット。</param>
        [RelayCommand]
        private async Task ApplyPreset(Preset preset)
        {
            if (preset == null) return;
            
            var currentSessions = await _audioService.GetActiveSessionsAsync(SelectedOutputDevice?.Id);
            await _presetService.ApplyPresetAsync(preset, currentSessions);
            
            // UIのマスター音量も更新
            if (preset.MasterVolume.HasValue)
            {
                MasterVolume = preset.MasterVolume.Value * 100f; // 0-1 を 0-100 に変換
            }
            
            await LoadSessionsAsync();
        }

        /// <summary>
        /// 指定したプリセットを削除します。
        /// </summary>
        /// <param name="preset">削除するプリセット。</param>
        [RelayCommand]
        private async Task DeletePreset(Preset preset)
        {
            if (preset == null || string.IsNullOrEmpty(preset.DeviceId)) return;
            
            await _presetService.DeletePresetAsync(preset.Name, preset.DeviceId);
            LoadPresets();
        }

        /// <summary>
        /// プリセットの名前を変更します。
        /// </summary>
        /// <param name="args">タプル (Preset, NewName)。</param>
        [RelayCommand]
        private async Task RenamePreset((Preset Preset, string NewName) args)
        {
            if (args.Preset == null || string.IsNullOrEmpty(args.NewName) || string.IsNullOrEmpty(args.Preset.DeviceId)) return;
            
            await _presetService.RenamePresetAsync(args.Preset.Name, args.Preset.DeviceId, args.NewName);
            LoadPresets();
        }

        /// <summary>
        /// プリセットをお気に入りに設定します。
        /// </summary>
        [RelayCommand]
        private async Task TogglePresetFavoriteAsync(Preset preset)
        {
            if (preset != null && SelectedOutputDevice != null)
            {
                preset.IsFavorite = !preset.IsFavorite;
                await _presetService.SetFavoriteAsync(preset.Name, SelectedOutputDevice.Id, preset.IsFavorite);
            }
        }

        /// <summary>
        /// 出力デバイスのミュートを切り替えます。
        /// </summary>
        [RelayCommand]
        private void ToggleOutputMute()
        {
            if (SelectedOutputDevice != null)
            {
                IsOutputMuted = !IsOutputMuted;
                _audioService.SetDeviceMasterMute(SelectedOutputDevice.Id, IsOutputMuted);
            }
        }

        /// <summary>
        /// 入力デバイスのミュートを切り替えます。
        /// </summary>
        [RelayCommand]
        private void ToggleInputMute()
        {
            if (SelectedInputDevice != null)
            {
                IsInputMuted = !IsInputMuted;
                _audioService.SetDeviceMasterMute(SelectedInputDevice.Id, IsInputMuted);
            }
        }

        /// <summary>
        /// テーマを変更します。
        /// </summary>
        /// <param name="themeName">変更するテーマ名。</param>
        [RelayCommand]
        private void ChangeTheme(string themeName)
        {
            if (!string.IsNullOrEmpty(themeName))
            {
                _themeService.SetTheme(themeName);
                OnPropertyChanged(nameof(CurrentTheme));
            }
        }
    }
}
