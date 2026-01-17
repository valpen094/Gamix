using CommunityToolkit.Mvvm.ComponentModel;
using Gamix.Core.Audio;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Gamix.UI.ViewModels
{
    public partial class SessionListViewModel : ObservableObject
    {
        private readonly IAudioService _audioService;
        private System.Threading.CancellationTokenSource? _sessionsChangedCts;

        public ObservableCollection<AudioSessionViewModel> Sessions { get; } = [];

        public SessionListViewModel(IAudioService audioService)
        {
            _audioService = audioService;
            _audioService.SessionsChanged += OnSessionsChanged;
        }

        public async Task LoadSessionsAsync(string? deviceId)
        {
            var sessions = await _audioService.GetActiveSessionsAsync(deviceId);
            Sessions.Clear();
            foreach (var s in sessions)
            {
                // マスターボリュームは上部に専用UIがあるため、セッションリストからは除外
                if (s.IsMaster) continue;
                Sessions.Add(new AudioSessionViewModel(s, _audioService));
            }
        }

        private void OnSessionsChanged()
        {
            // セッション変更イベントのデバウンス
            _sessionsChangedCts?.Cancel();
            _sessionsChangedCts = new System.Threading.CancellationTokenSource();
            var token = _sessionsChangedCts.Token;

            Task.Delay(300, token).ContinueWith(async _ =>
            {
                if (token.IsCancellationRequested) return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // Note: MainViewModel needs to tell us WHICH device ID to load.
                    // Ideally, we store currentDeviceId here or expose an Event/Command to request refresh.
                    // For now, let's expose an event "RefreshRequested" or similar, or just let MainViewModel handle the orchestration if it owns this VM.
                    // But if MainViewModel owns this VM, MainViewModel should subscribe to this VM's need to refresh?
                    // Actually, OnSessionsChanged comes from AudioService.
                    // We need the current Device ID to reload.
                    // Let's store CurrentDeviceId property here.
                    if (_currentDeviceId != null)
                    {
                        await LoadSessionsAsync(_currentDeviceId);
                    }
                });
            }, TaskScheduler.Default);
        }

        private string? _currentDeviceId;
        public void SetCurrentDeviceId(string? deviceId)
        {
            _currentDeviceId = deviceId;
        }
    }
}
