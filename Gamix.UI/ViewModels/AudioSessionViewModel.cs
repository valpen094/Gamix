using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.Core.Audio;
using Gamix.Core.Models;
using Gamix.UI.Converters;
using System.Windows.Media;

namespace Gamix.UI.ViewModels
{
    /// <summary>
    /// AudioSession モデルをラップする ViewModel。
    /// 音量変更時にオーディオサービスへ即座に反映します。
    /// </summary>
    public partial class AudioSessionViewModel : ObservableObject
    {
        private readonly AudioSession _model;
        private readonly IAudioService _audioService;

        /// <summary>
        /// AudioSessionViewModel のコンストラクタ。
        /// </summary>
        /// <param name="model">ラップする AudioSession モデル。</param>
        /// <param name="audioService">音量変更に使用する IAudioService。</param>
        public AudioSessionViewModel(AudioSession model, IAudioService audioService)
        {
            _model = model;
            _audioService = audioService;
        }

        /// <summary>
        /// プロセス名。
        /// </summary>
        public string ProcessName => _model.ProcessName;

        /// <summary>
        /// UI表示用の名前。
        /// </summary>
        public string DisplayName => _model.DisplayName;

        /// <summary>
        /// セッションID。
        /// </summary>
        public string Id => _model.Id;

        /// <summary>
        /// ミュート状態かどうか。
        /// </summary>
        public bool IsMuted
        {
            get => _model.IsMuted;
            set
            {
                if (SetProperty(_model.IsMuted, value, _model, (m, v) => m.IsMuted = v))
                {
                    _audioService.SetMute(_model.Id, value);
                }
            }
        }

        /// <summary>
        /// アイコンパス。
        /// </summary>
        public string IconPath => _model.IconPath;

        /// <summary>
        /// アプリケーションアイコン。
        /// </summary>
        public ImageSource? Icon => IconHelper.GetIconFromPath(_model.IconPath, _model.IsMaster, _model.ProcessName, _model.MainWindowHandle);

        /// <summary>
        /// マスター音量かどうか。
        /// </summary>
        public bool IsMaster => _model.IsMaster;

        /// <summary>
        /// 音量レベル (0.0 〜 1.0)。変更時に自動でオーディオサービスへ反映されます。
        /// </summary>
        public float Volume
        {
            get => _model.Volume;
            set
            {
                if (SetProperty(_model.Volume, value, _model, (m, v) => m.Volume = v))
                {
                    _audioService.SetVolume(_model.Id, _model.Volume);
                }
            }
        }
        /// <summary>
        /// ミュート状態を切り替えます。
        /// </summary>
        [RelayCommand]
        private void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        /// <summary>
        /// 外部からの変更通知を受け取り、ViewModelの状態を更新します。
        /// 循環呼び出しを防ぐため、サービスへの通知は行いません。
        /// </summary>
        public void UpdateVolume(float volume, bool isMuted)
        {
            if (_model.Volume != volume)
            {
                _model.Volume = volume;
                OnPropertyChanged(nameof(Volume));
            }

            if (_model.IsMuted != isMuted)
            {
                _model.IsMuted = isMuted;
                OnPropertyChanged(nameof(IsMuted));
            }
        }
    }
}

