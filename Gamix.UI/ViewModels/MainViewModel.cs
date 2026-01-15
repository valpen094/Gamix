using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.UI.Services;
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
        
        /// <summary>
        /// アクティブなオーディオセッションのコレクション。
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<AudioSessionViewModel> Sessions { get; } = new();

        /// <summary>
        /// 保存されたプリセットのコレクション。
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<Gamix.Core.Models.Preset> Presets { get; } = new();

        /// <summary>
        /// 新規プリセット名の入力値。
        /// </summary>
        [ObservableProperty]
        private string _newPresetName = string.Empty;

        /// <summary>
        /// MainViewModel のコンストラクタ。
        /// </summary>
        /// <param name="audioService">オーディオセッション取得用のサービス。</param>
        /// <param name="presetService">プリセット管理用のサービス。</param>
        public MainViewModel(Gamix.Core.Audio.IAudioService audioService, Gamix.Core.Services.IPresetService presetService)
        {
            _audioService = audioService;
            _presetService = presetService;
            LoadSessions();
            LoadPresets();
        }

        /// <summary>
        /// オーディオセッションを読み込みます。
        /// </summary>
        private async void LoadSessions()
        {
            var sessions = await _audioService.GetActiveSessionsAsync();
            Sessions.Clear();
            foreach (var s in sessions)
            {
                Sessions.Add(new AudioSessionViewModel(s, _audioService));
            }
        }

        /// <summary>
        /// プリセットを読み込みます。
        /// </summary>
        private async void LoadPresets()
        {
            var presets = await _presetService.LoadPresetsAsync();
            Presets.Clear();
            foreach (var p in presets) Presets.Add(p);
        }

        /// <summary>
        /// 現在の音量状態をプリセットとして保存します。
        /// </summary>
        [RelayCommand]
        private async Task SavePreset()
        {
            if (string.IsNullOrWhiteSpace(NewPresetName)) return;
            var currentModels = Sessions.Select(s => new Gamix.Core.Models.AudioSession 
            { 
                Id = s.Id, 
                ProcessName = s.ProcessName, 
                Volume = s.Volume, 
                IsMuted = s.IsMuted 
            }).ToList();
            
            await _presetService.SavePresetAsync(NewPresetName, currentModels);
            NewPresetName = string.Empty;
            LoadPresets();
        }

        /// <summary>
        /// 指定したプリセットを適用します。
        /// </summary>
        /// <param name="preset">適用するプリセット。</param>
        [RelayCommand]
        private async Task ApplyPreset(Gamix.Core.Models.Preset preset)
        {
            if (preset == null) return;
            
            var currentSessions = await _audioService.GetActiveSessionsAsync();
            await _presetService.ApplyPresetAsync(preset, currentSessions);
            
            LoadSessions();
        }

        /// <summary>
        /// 指定したプリセットを削除します。
        /// </summary>
        /// <param name="preset">削除するプリセット。</param>
        [RelayCommand]
        private async Task DeletePreset(Gamix.Core.Models.Preset preset)
        {
            if (preset == null) return;
            
            await _presetService.DeletePresetAsync(preset.Name);
            LoadPresets();
        }
    }
}
