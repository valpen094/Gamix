using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gamix.Core.Audio;
using Gamix.Core.Models;

namespace Gamix.Core.Services
{
    /// <summary>
    /// JSON ファイルを使用した IPresetService の実装。
    /// プリセットは %AppData%/Gamix/presets.json に保存されます。
    /// </summary>
    public class PresetService : IPresetService
    {
        private readonly string _filePath;
        private readonly IAudioService _audioService;

        /// <summary>
        /// PresetService のコンストラクタ。
        /// </summary>
        /// <param name="audioService">音量設定に使用する IAudioService。</param>
        public PresetService(IAudioService audioService)
        {
            _audioService = audioService;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "Gamix");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "presets.json");
        }

        /// <inheritdoc/>
        public async Task SavePresetAsync(string name, string deviceId, float masterVolume, bool isMasterMuted, List<AudioSession> currentSessions)
        {
            var presets = await LoadPresetsAsync();
            var newPreset = new Preset
            {
                Name = name,
                DeviceId = deviceId,
                MasterVolume = masterVolume,
                IsMasterMuted = isMasterMuted,
                Settings = currentSessions.Select(s => new ProcessVolumeSetting
                {
                    ProcessName = s.ProcessName,
                    Volume = s.Volume,
                    IsMuted = s.IsMuted
                }).ToList()
            };

            // 同名かつ同デバイスのプリセットがあれば上書き
            presets.RemoveAll(p => p.Name == name && p.DeviceId == deviceId);
            presets.Add(newPreset);

            await SaveToFileAsync(presets);
        }

        /// <inheritdoc/>
        public async Task<List<Preset>> LoadPresetsAsync()
        {
            if (!File.Exists(_filePath)) return new List<Preset>();

            try
            {
                using var stream = File.OpenRead(_filePath);
                return await JsonSerializer.DeserializeAsync<List<Preset>>(stream) ?? new List<Preset>();
            }
            catch
            {
                return new List<Preset>();
            }
        }

        /// <inheritdoc/>
        public async Task ApplyPresetAsync(Preset preset, List<AudioSession> currentSessions)
        {
            // マスター音量を復元
            if (preset.MasterVolume.HasValue && !string.IsNullOrEmpty(preset.DeviceId))
            {
                _audioService.SetDeviceMasterVolume(preset.DeviceId, preset.MasterVolume.Value);
            }
            if (preset.IsMasterMuted.HasValue && !string.IsNullOrEmpty(preset.DeviceId))
            {
                _audioService.SetDeviceMasterMute(preset.DeviceId, preset.IsMasterMuted.Value);
            }

            // 各アプリの音量を復元
            foreach (var setting in preset.Settings)
            {
                var target = currentSessions.FirstOrDefault(s => s.ProcessName == setting.ProcessName);
                if (target != null)
                {
                    _audioService.SetVolume(target.Id, setting.Volume);
                    _audioService.SetMute(target.Id, setting.IsMuted);
                }
            }
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DeletePresetAsync(string name, string deviceId)
        {
            var presets = await LoadPresetsAsync();
            presets.RemoveAll(p => p.Name == name && p.DeviceId == deviceId);
            await SaveToFileAsync(presets);
        }

        /// <inheritdoc/>
        public async Task RenamePresetAsync(string oldName, string deviceId, string newName)
        {
            var presets = await LoadPresetsAsync();
            var target = presets.FirstOrDefault(p => p.Name == oldName && p.DeviceId == deviceId);
            if (target != null)
            {
                target.Name = newName;
                await SaveToFileAsync(presets);
            }
        }

        /// <inheritdoc/>
        public async Task SetFavoriteAsync(string name, string deviceId, bool isFavorite)
        {
            var presets = await LoadPresetsAsync();
            
            // 同じデバイスの他のプリセットのお気に入りを解除
            if (isFavorite)
            {
                foreach (var p in presets.Where(p => p.DeviceId == deviceId))
                {
                    p.IsFavorite = false;
                }
            }
            
            // 対象プリセットのお気に入り状態を設定
            var target = presets.FirstOrDefault(p => p.Name == name && p.DeviceId == deviceId);
            if (target != null)
            {
                target.IsFavorite = isFavorite;
                await SaveToFileAsync(presets);
            }
        }

        /// <inheritdoc/>
        public async Task<Preset?> GetFavoritePresetAsync(string deviceId)
        {
            var presets = await LoadPresetsAsync();
            return presets.FirstOrDefault(p => p.DeviceId == deviceId && p.IsFavorite);
        }

        /// <summary>
        /// プリセット一覧をファイルに保存します。
        /// </summary>
        /// <param name="presets">保存するプリセット一覧。</param>
        private async Task SaveToFileAsync(List<Preset> presets)
        {
            using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, presets);
        }
    }
}
