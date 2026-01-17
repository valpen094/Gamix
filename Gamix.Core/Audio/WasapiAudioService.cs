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
            Console.WriteLine("[WasapiAudioService] Firing SessionsChanged event.");
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
                                session.ProcessName = proc.ProcessName;
                                session.IconPath = proc.MainModule?.FileName ?? "";
                            }
                            catch 
                            {
                                session.ProcessName = $"PID: {pid}";
                            }
                        }
                        else
                        {
                            session.ProcessName = "System Sounds";
                        }
                        
                        sessions.Add(session);
                    }
                    
                    // マスター音量を先頭に追加
                    sessions.Insert(0, new AudioSession 
                    { 
                        Id = "Master", 
                        ProcessName = "Master Volume", 
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
        private readonly List<(AudioSessionControl Session, SessionEventsListener Listener)> _monitoredWrapperSessions = new();
        private readonly object _lock = new object();

        /// <inheritdoc/>
        public void StartSessionMonitoring(string deviceId)
        {
            Console.WriteLine($"[WasapiAudioService] StartSessionMonitoring: {deviceId}");
            // Check if we are already monitoring and the device is alive
            if (_monitoringDeviceId == deviceId && _currentSessionManager != null && _monitoringDevice != null) 
            {
                Console.WriteLine("[WasapiAudioService] Already monitoring this device.");
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
                Console.WriteLine("[WasapiAudioService] Subscribed to OnSessionCreated after RefreshSessions.");
                
                // 既存セッションも監視
                var sessions = _currentSessionManager.Sessions;
                Console.WriteLine($"[WasapiAudioService] Found {sessions.Count} existing sessions.");
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    RegisterSessionEvents(session);
                }

                _monitoringDeviceId = deviceId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WasapiAudioService] Error starting session monitoring: {ex.Message}");
            }
        }

        private void StopSessionMonitoring()
        {
            Console.WriteLine("[WasapiAudioService] StopSessionMonitoring");
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
                    catch { /* 無視 */ }
                }
                _monitoredWrapperSessions.Clear();
            }
            
            _monitoringDeviceId = null;
        }

        private void OnSessionCreated(object? sender, IAudioSessionControl e)
        {
            Console.WriteLine("[WasapiAudioService] OnSessionCreated fired!");
            try
            {
                // NAudio の AudioSessionControl ラッパーを作成
                var wrapper = new AudioSessionControl(e);
                RegisterSessionEvents(wrapper);
                OnSessionsChanged();
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"[WasapiAudioService] OnSessionCreated Error: {ex.Message}");
            }
        }

        private void RegisterSessionEvents(AudioSessionControl session)
        {
            try 
            {
                Console.WriteLine($"[WasapiAudioService] Registering events for session: {session.GetSessionIdentifier} State:{session.State}");
                if (session.State == AudioSessionState.AudioSessionStateExpired) return;

                var listener = new SessionEventsListener(this);
                session.RegisterEventClient(listener);

                lock (_lock)
                {
                    _monitoredWrapperSessions.Add((session, listener));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WasapiAudioService] RegisterSessionEvents Error: {ex.Message}");
            }
        }

        public void HandleSessionEvent()
        {
            OnSessionsChanged();
        }
        
        // IAudioSessionEventsHandler implementation
        private class SessionEventsListener : IAudioSessionEventsHandler
        {
            private readonly WasapiAudioService _service;
            public SessionEventsListener(WasapiAudioService service) { _service = service; }
            
            public void OnDisplayNameChanged(string displayName) { }
            public void OnIconPathChanged(string iconPath) { }
            public void OnVolumeChanged(float volume, bool isMuted) { }
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
