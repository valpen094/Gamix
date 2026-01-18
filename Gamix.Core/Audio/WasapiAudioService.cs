using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gamix.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Gamix.Core.Audio
{
    /// <summary>
    /// Windows Core Audio API (WASAPI) を使用した IAudioService の実装。
    /// </summary>
    public class WasapiAudioService : IAudioService, IDisposable
    {
        private readonly MMDeviceEnumerator _enumerator;

        public WasapiAudioService()
        {
            _enumerator = new MMDeviceEnumerator();
            RegisterNotificationClient();
        }

        /// <inheritdoc/>
        public event Action? SessionsChanged;
        
        /// <summary>
        /// SessionsChanged イベントを発火します。
        /// </summary>
        protected virtual void OnSessionsChanged()
        {
            SessionsChanged?.Invoke();
        }

        /// <inheritdoc/>
        public Task<List<AudioSession>> GetActiveSessionsAsync()
        {
            return GetActiveSessionsAsync(null);
        }

        /// <inheritdoc/>
        public Task<List<AudioSession>> GetActiveSessionsAsync(string? deviceId)
        {
            return Task.Run(() =>
            {
                var sessions = new List<AudioSession>();
                
                try 
                {
                    var device = string.IsNullOrEmpty(deviceId)
                        ? _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                        : _enumerator.GetDevice(deviceId);
                    
                    var sessionManager = device.AudioSessionManager;

                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var ctl = sessionManager.Sessions[i];
                        
                        if (ctl.State == AudioSessionState.AudioSessionStateExpired) continue;

                        var session = new AudioSession
                        {
                            Id = ctl.GetSessionIdentifier,
                            Volume = ctl.SimpleAudioVolume.Volume,
                            IsMuted = ctl.SimpleAudioVolume.Mute
                        };

                        uint pid = ctl.GetProcessID;
                        if (pid > 0)
                        {
                            session.ProcessId = (int)pid;
                            try 
                            {
                                var proc = Process.GetProcessById((int)pid);
                                var processName = proc.ProcessName;
                                
                                // Map system host processes to friendly name
                                session.ProcessName = IsSystemSoundProcess(processName) 
                                    ? "System Sounds" 
                                    : processName;
                                
                                // Try to get executable path
                                string path = "";
                                try 
                                { 
                                    path = proc.MainModule?.FileName ?? ""; 
                                    session.IconPath = path;
                                } 
                                catch 
                                { 
                                    session.IconPath = "";
                                }

                                // Determine DisplayName
                                session.DisplayName = GetSessionDisplayName(proc, path, session.ProcessName);

                                try { session.MainWindowHandle = proc.MainWindowHandle; } catch { }
                            }
                            catch 
                            {
                                session.ProcessName = $"PID: {pid}";
                                session.DisplayName = session.ProcessName;
                            }
                        }
                        else
                        {
                            session.ProcessName = "System Sounds";
                            session.DisplayName = "System Sounds";
                        }
                        
                        sessions.Add(session);
                    }
                    
                    // マスター音量を先頭に追加
                    sessions.Insert(0, new AudioSession 
                    { 
                        Id = "Master", 
                        ProcessName = "Master Volume", 
                        DisplayName = "Master Volume",
                        IsMaster = true,
                        Volume = device.AudioEndpointVolume.MasterVolumeLevelScalar,
                        IsMuted = device.AudioEndpointVolume.Mute
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error enumerating sessions: {ex.Message}");
                }

                return sessions;
            });
        }

        /// <summary>
        /// Determines the best user-friendly display name for a session.
        /// Prioritizes Window Title > FileDescription > ProductName > ProcessName.
        /// </summary>
        private static string GetSessionDisplayName(Process proc, string? filePath, string fallbackProcessName)
        {
            string? displayName = null;

            // 1. Try WindowTitle (Most specific, e.g. "Game Title")
            try 
            {
                var title = proc.MainWindowTitle;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    displayName = title;
                }
            } 
            catch { }

            // 2. Fallback to FileDescription / ProductName from file
            if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrEmpty(filePath))
            {
                try 
                {
                    var info = FileVersionInfo.GetVersionInfo(filePath);
                    
                    // Try FileDescription
                    var fileDesc = info.FileDescription;
                    if (IsValidDisplayName(fileDesc))
                    {
                        displayName = fileDesc;
                    }
                    
                    // Fallback to ProductName
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        var prodName = info.ProductName;
                        if (IsValidDisplayName(prodName))
                        {
                            displayName = prodName;
                        }
                    }
                } 
                catch { }
            }

            // 3. Last resort: Fallback to ProcessName
            return string.IsNullOrWhiteSpace(displayName) 
                ? fallbackProcessName 
                : displayName;
        }

        /// <summary>
        /// Checks if the display name is valid and user-friendly.
        /// Rejects names that look like log files, temp files, or generic system/engine names.
        /// </summary>
        private static bool IsValidDisplayName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            // Reject if looks like a file extension
            if (name.EndsWith(".log", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Reject generic engine/client names
            if (name.Contains("Engine", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Game Client", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Bootstrap", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the given process name is a known Windows system sound host process.
        /// </summary>
        private static bool IsSystemSoundProcess(string processName)
        {
            // Only map taskhostw explicitly requested by user
            return string.Equals(processName, "taskhostw", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public Task<List<AudioDevice>> GetAudioDevicesAsync(bool isOutput)
        {
            return Task.Run(() =>
            {
                var devices = new List<AudioDevice>();
                
                try
                {
                    var dataFlow = isOutput ? DataFlow.Render : DataFlow.Capture;
                    var mmDevices = _enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);
                    var defaultDevice = _enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);

                    foreach (var mmDevice in mmDevices)
                    {
                        devices.Add(new AudioDevice
                        {
                            Id = mmDevice.ID,
                            Name = mmDevice.FriendlyName,
                            IsDefault = mmDevice.ID == defaultDevice.ID,
                            IsOutput = isOutput
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error enumerating devices: {ex.Message}");
                }

                return devices;
            });
        }

        /// <inheritdoc/>
        public Task<(float Volume, bool IsMuted)> GetDeviceMasterVolumeAsync(string deviceId)
        {
            return Task.Run(() =>
            {
                try
                {
                    var device = _enumerator.GetDevice(deviceId);
                    return (device.AudioEndpointVolume.MasterVolumeLevelScalar, device.AudioEndpointVolume.Mute);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting master volume: {ex.Message}");
                    return (0f, false);
                }
            });
        }

        /// <inheritdoc/>
        public void SetDeviceMasterVolume(string deviceId, float volume)
        {
            var device = _enumerator.GetDevice(deviceId);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }

        /// <inheritdoc/>
        public void SetDeviceMasterMute(string deviceId, bool isMuted)
        {
            var device = _enumerator.GetDevice(deviceId);
            device.AudioEndpointVolume.Mute = isMuted;
        }

        /// <inheritdoc/>
        public void SetVolume(string sessionId, float volume)
        {
            var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (sessionId == "Master")
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                return;
            }

            var sessionManager = device.AudioSessionManager;
            for (int i = 0; i < sessionManager.Sessions.Count; i++)
            {
                var ctl = sessionManager.Sessions[i];
                if (ctl.GetSessionIdentifier == sessionId)
                {
                    ctl.SimpleAudioVolume.Volume = volume;
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public void SetMute(string sessionId, bool isMuted)
        {
            var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            if (sessionId == "Master")
            {
                device.AudioEndpointVolume.Mute = isMuted;
                return;
            }

            var sessionManager = device.AudioSessionManager;
            for (int i = 0; i < sessionManager.Sessions.Count; i++)
            {
                var ctl = sessionManager.Sessions[i];
                if (ctl.GetSessionIdentifier == sessionId)
                {
                    ctl.SimpleAudioVolume.Mute = isMuted;
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public event Action? DevicesChanged;

        /// <summary>
        /// DevicesChanged イベントを発火します。
        /// </summary>
        protected virtual void OnDevicesChanged()
        {
            DevicesChanged?.Invoke();
        }

        public event Action<string, float, bool>? SessionVolumeChanged;

        protected virtual void OnSessionVolumeChanged(string sessionId, float volume, bool isMuted)
        {
            SessionVolumeChanged?.Invoke(sessionId, volume, isMuted);
        }

        private NotificationClient? _notificationClient;

        private void RegisterNotificationClient()
        {
            if (_notificationClient == null)
            {
                _notificationClient = new NotificationClient(this);
                _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
            }
        }

        private void UnregisterNotificationClient()
        {
            if (_notificationClient != null)
            {
                _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                _notificationClient = null;
            }
        }

        private AudioSessionManager? _currentSessionManager;
        private MMDevice? _monitoringDevice;
        private string? _monitoringDeviceId;
        private readonly List<(AudioSessionControl Session, SessionEventsListener Listener)> _monitoredWrapperSessions = [];
        private readonly object _lock = new();

        /// <inheritdoc/>
        public void StartSessionMonitoring(string deviceId)
        {
            // Check if we are already monitoring and the device is alive
            if (_monitoringDeviceId == deviceId && _currentSessionManager != null && _monitoringDevice != null) 
            {
                return;
            }

            StopSessionMonitoring();

            try
            {
                var device = _enumerator.GetDevice(deviceId);
                _monitoringDevice = device;
                _currentSessionManager = device.AudioSessionManager;
                
                // NAudioの内部実装では、RefreshSessions()がIAudioSessionNotificationを登録する
                // AudioSessionManagerのコンストラクタで一度呼ばれるが、明示的に呼び出して確実にする
                _currentSessionManager.RefreshSessions();
                
                _currentSessionManager.OnSessionCreated += OnSessionCreated;
                
                // 既存セッションも監視
                var sessions = _currentSessionManager.Sessions;
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    RegisterSessionEvents(session);
                }

                _monitoringDeviceId = deviceId;
            }
            catch (Exception)
            {
                // セッション監視の開始に失敗しても、アプリ自体は動作を継続する
            }
        }

        private void StopSessionMonitoring()
        {
            if (_currentSessionManager != null)
            {
                _currentSessionManager.OnSessionCreated -= OnSessionCreated;
                _currentSessionManager = null;
            }
            
            lock (_lock)
            {
                foreach (var (session, listener) in _monitoredWrapperSessions)
                {
                    try 
                    {
                        session.UnRegisterEventClient(listener);
                        session.Dispose();
                    }
                    catch
                    {
                        // セッション解除に失敗しても継続
                    }
                }
                _monitoredWrapperSessions.Clear();
            }
            
            _monitoringDeviceId = null;
        }

        private void OnSessionCreated(object? sender, IAudioSessionControl e)
        {
            try
            {
                // NAudio の AudioSessionControl ラッパーを作成
                var wrapper = new AudioSessionControl(e);
                RegisterSessionEvents(wrapper);
                OnSessionsChanged();
            }
            catch (Exception)
            {
                // セッション作成イベントの処理に失敗しても継続
            }
        }

        private void RegisterSessionEvents(AudioSessionControl session)
        {
            try 
            {
                if (session.State == AudioSessionState.AudioSessionStateExpired) return;

                var listener = new SessionEventsListener(this, session.GetSessionIdentifier);
                session.RegisterEventClient(listener);

                lock (_lock)
                {
                    _monitoredWrapperSessions.Add((session, listener));
                }
            }
            catch (Exception)
            {
                // イベント登録に失敗しても継続
            }
        }

        public void HandleSessionEvent()
        {
            OnSessionsChanged();
        }

        public void HandleSessionVolumeEvent(string sessionId, float volume, bool isMuted)
        {
            OnSessionVolumeChanged(sessionId, volume, isMuted);
        }
        
        // IAudioSessionEventsHandler implementation
        private class SessionEventsListener : IAudioSessionEventsHandler
        {
            private readonly WasapiAudioService _service;
            private readonly string _sessionId;

            public SessionEventsListener(WasapiAudioService service, string sessionId) 
            { 
                _service = service; 
                _sessionId = sessionId;
            }
            
            public void OnDisplayNameChanged(string displayName) { }
            public void OnIconPathChanged(string iconPath) { }
            public void OnVolumeChanged(float volume, bool isMuted) 
            {
                _service.HandleSessionVolumeEvent(_sessionId, volume, isMuted);
            }
            public void OnChannelVolumeChanged(uint channelCount, IntPtr newChannelVolumeArray, uint changedChannel) { }
            public void OnGroupingParamChanged(ref Guid groupingId) { }
            public void OnStateChanged(AudioSessionState state) 
            { 
                _service.HandleSessionEvent();
            }
            public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) 
            {
                 _service.HandleSessionEvent();
            }
        }
        
        public void Dispose()
        {
            StopSessionMonitoring();
            UnregisterNotificationClient();
            _enumerator?.Dispose();
            GC.SuppressFinalize(this);
        }

        private class NotificationClient : IMMNotificationClient
        {
            private readonly WasapiAudioService _parent;

            public NotificationClient(WasapiAudioService parent)
            {
                _parent = parent;
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                _parent.OnDevicesChanged();
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                _parent.OnDevicesChanged();
            }

            public void OnDeviceRemoved(string deviceId)
            {
                _parent.OnDevicesChanged();
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                _parent.OnDevicesChanged();
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                // 無視
            }
        }
    }
}
