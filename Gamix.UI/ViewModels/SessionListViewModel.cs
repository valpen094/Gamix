using CommunityToolkit.Mvvm.ComponentModel;
using Gamix.Core.Audio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gamix.UI.ViewModels
{
    public partial class SessionListViewModel : ObservableObject, IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly Dictionary<string, AudioSessionViewModel> _sessionMap = new();
        private readonly object _lock = new();
        private CancellationTokenSource? _sessionsChangedCts;

        public ObservableCollection<AudioSessionViewModel> Sessions { get; } = [];

        [ObservableProperty]
        private string? _currentDeviceId;

        public SessionListViewModel(IAudioService audioService)
        {
            _audioService = audioService;
            _audioService.SessionsChanged += OnSessionsChanged;
            _audioService.SessionVolumeChanged += OnSessionVolumeChanged;
        }

        public async Task LoadSessionsAsync(string? deviceId)
        {
            var sessions = await _audioService.GetActiveSessionsAsync(deviceId);
            
            lock (_lock)
            {
                Sessions.Clear();
                _sessionMap.Clear();

                foreach (var s in sessions)
                {
                    if (s.IsMaster) continue;

                    var viewModel = new AudioSessionViewModel(s, _audioService);
                    Sessions.Add(viewModel);
                    _sessionMap[s.Id] = viewModel;
                }
            }
        }

        private void OnSessionsChanged()
        {
            DebounceAndReload(300);
        }

        private void OnSessionVolumeChanged(string sessionId, float volume, bool isMuted)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                lock (_lock)
                {
                    if (_sessionMap.TryGetValue(sessionId, out var sessionVm))
                    {
                        sessionVm.UpdateVolume(volume, isMuted);
                    }
                }
            });
        }

        private void DebounceAndReload(int delayMs)
        {
            _sessionsChangedCts?.Cancel();
            _sessionsChangedCts = new CancellationTokenSource();
            var token = _sessionsChangedCts.Token;

            Task.Delay(delayMs, token).ContinueWith(async _ =>
            {
                if (token.IsCancellationRequested) return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (CurrentDeviceId != null)
                    {
                        await LoadSessionsAsync(CurrentDeviceId);
                    }
                });
            }, TaskScheduler.Default);
        }

        public void Dispose()
        {
            _audioService.SessionsChanged -= OnSessionsChanged;
            _audioService.SessionVolumeChanged -= OnSessionVolumeChanged;
            _sessionsChangedCts?.Cancel();
            _sessionsChangedCts?.Dispose();
        }
    }
}
