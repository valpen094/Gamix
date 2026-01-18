using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.Core.Models;
using Gamix.Core.Services;
using Gamix.Core.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Gamix.UI.ViewModels
{
    /// <summary>
    /// メインウィンドウ用の ViewModel。
    /// 各機能の ViewModel (Device, Session, Theme) を統括し、プリセット管理を提供します。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly Gamix.Core.Audio.IAudioService _audioService;
        private readonly IPresetService _presetService;

        public DeviceViewModel Devices { get; }
        public SessionListViewModel Sessions { get; }
        public ThemeViewModel Themes { get; }

        /// <summary>
        /// プリセットが適用されたときに発火するイベント。
        /// </summary>
        public event Action? PresetApplied;

        /// <summary>
        /// 保存されたプリセットのコレクション。
        /// </summary>
        public ObservableCollection<Preset> Presets { get; } = [];

        /// <summary>
        /// 新規プリセット名の入力値。
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SavePresetCommand))]
        private string _newPresetName = string.Empty;

        public MainViewModel(
            Gamix.Core.Audio.IAudioService audioService, 
            IPresetService presetService,
            DeviceViewModel deviceViewModel,
            SessionListViewModel sessionListViewModel,
            ThemeViewModel themeViewModel)
        {
            _audioService = audioService;
            _presetService = presetService;
            Devices = deviceViewModel;
            Sessions = sessionListViewModel;
            Themes = themeViewModel;

            Devices.PropertyChanged += Devices_PropertyChanged;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await Devices.InitializeAsync();
            
            // 起動時に Favorite プリセットを自動適用
            await ApplyFavoritePresetAsync();

            LoadPresets();
            
            if (Devices.SelectedOutputDevice != null)
            {
                await Sessions.LoadSessionsAsync(Devices.SelectedOutputDevice.Id);
                Sessions.CurrentDeviceId = Devices.SelectedOutputDevice.Id;
                _audioService.StartSessionMonitoring(Devices.SelectedOutputDevice.Id);
            }
        }

        private void Devices_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceViewModel.SelectedOutputDevice))
            {
                var deviceId = Devices.SelectedOutputDevice?.Id;
                
                // セッションリスト更新
                _ = Sessions.LoadSessionsAsync(deviceId);
                Sessions.CurrentDeviceId = deviceId;
                if (deviceId != null) _audioService.StartSessionMonitoring(deviceId);

                // プリセットリスト更新
                LoadPresets();
                NewPresetName = string.Empty;
            }
        }

        /// <summary>
        /// Favorite プリセットを自動適用します。
        /// </summary>
        private async Task ApplyFavoritePresetAsync()
        {
            if (Devices.SelectedOutputDevice == null) return;
            
            var favorite = await _presetService.GetFavoritePresetAsync(Devices.SelectedOutputDevice.Id);
            if (favorite != null)
            {
                await ApplyPreset(favorite);
            }
        }

        /// <summary>
        /// プリセットを読み込みます。
        /// 選択中の出力デバイスに関連するプリセットのみを表示します。
        /// </summary>
        private async void LoadPresets()
        {
            var allPresets = await _presetService.LoadPresetsAsync();
            var deviceId = Devices.SelectedOutputDevice?.Id;
            
            Presets.Clear();
            foreach (var p in allPresets)
            {
                if (p.DeviceId == deviceId)
                {
                    Presets.Add(p);
                }
            }
        }

        private bool CanSavePreset => !string.IsNullOrWhiteSpace(NewPresetName);

        /// <summary>
        /// 現在の音量状態をプリセットとして保存します。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSavePreset))]
        private async Task SavePreset()
        {
            if (string.IsNullOrWhiteSpace(NewPresetName) || Devices.SelectedOutputDevice == null) return;
            
            // 文字数制限 (見た目の5文字)
            var name = NewPresetName.TruncateVisual(5);
            
            var currentModels = Sessions.Sessions.Select(s => new AudioSession 
            { 
                Id = s.Id, 
                ProcessName = s.ProcessName, 
                Volume = s.Volume, 
                IsMuted = s.IsMuted 
            }).ToList();
            
            await _presetService.SavePresetAsync(
                name, 
                Devices.SelectedOutputDevice.Id, 
                Devices.MasterVolume / 100f, 
                false, 
                currentModels);

            LoadPresets();
        }

        /// <summary>
        /// 指定したプリセットを適用します。
        /// </summary>
        [RelayCommand]
        private async Task ApplyPreset(Preset preset)
        {
            if (preset == null) return;
            
            var currentSessions = await _audioService.GetActiveSessionsAsync(Devices.SelectedOutputDevice?.Id);
            await _presetService.ApplyPresetAsync(preset, currentSessions);
            
            if (preset.MasterVolume.HasValue)
            {
                Devices.MasterVolume = preset.MasterVolume.Value * 100f;
            }
            
            NewPresetName = preset.Name;

            await Sessions.LoadSessionsAsync(Devices.SelectedOutputDevice?.Id);

            PresetApplied?.Invoke();
        }

        /// <summary>
        /// 指定したプリセットを削除します。
        /// </summary>
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
        [RelayCommand]
        private async Task RenamePreset((Preset Preset, string NewName) args)
        {
            if (args.Preset == null || string.IsNullOrWhiteSpace(args.NewName) || string.IsNullOrEmpty(args.Preset.DeviceId)) return;
            
            var newName = args.NewName.TruncateVisual(5);
            
            await _presetService.RenamePresetAsync(args.Preset.Name, args.Preset.DeviceId, newName);
            LoadPresets();
        }

        /// <summary>
        /// プリセットをお気に入りに設定します。
        /// </summary>
        [RelayCommand]
        private async Task TogglePresetFavoriteAsync(Preset preset)
        {
            if (preset != null && Devices.SelectedOutputDevice != null)
            {
                preset.IsFavorite = !preset.IsFavorite;
                await _presetService.SetFavoriteAsync(preset.Name, Devices.SelectedOutputDevice.Id, preset.IsFavorite);
            }
        }
    }
}
