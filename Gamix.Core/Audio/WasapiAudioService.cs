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
            return Task.Run(() =>
            {
                var sessions = new List<AudioSession>();
                
                try 
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
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
