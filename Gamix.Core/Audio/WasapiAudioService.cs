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
    public class WasapiAudioService : IAudioService
    {
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
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var device = string.IsNullOrEmpty(deviceId)
                            ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                            : enumerator.GetDevice(deviceId);
                        
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
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var dataFlow = isOutput ? DataFlow.Render : DataFlow.Capture;
                        var mmDevices = enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);
                        var defaultDevice = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);

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
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var device = enumerator.GetDevice(deviceId);
                        return (device.AudioEndpointVolume.MasterVolumeLevelScalar, device.AudioEndpointVolume.Mute);
                    }
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
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = enumerator.GetDevice(deviceId);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            }
        }

        /// <inheritdoc/>
        public void SetDeviceMasterMute(string deviceId, bool isMuted)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = enumerator.GetDevice(deviceId);
                device.AudioEndpointVolume.Mute = isMuted;
            }
        }

        /// <inheritdoc/>
        public void SetVolume(string sessionId, float volume)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

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
        }

        /// <inheritdoc/>
        public void SetMute(string sessionId, bool isMuted)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

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
        }
    }
}
