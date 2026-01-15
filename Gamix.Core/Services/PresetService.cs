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
        public async Task SavePresetAsync(string name, List<AudioSession> currentSessions)
        {
            var presets = await LoadPresetsAsync();
            var newPreset = new Preset
            {
                Name = name,
                Settings = currentSessions.Select(s => new ProcessVolumeSetting
                {
                    ProcessName = s.ProcessName,
                    Volume = s.Volume,
                    IsMuted = s.IsMuted
                }).ToList()
            };

            // 同名のプリセットがあれば上書き
            presets.RemoveAll(p => p.Name == name);
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
        public async Task DeletePresetAsync(string name)
        {
            var presets = await LoadPresetsAsync();
            presets.RemoveAll(p => p.Name == name);
            await SaveToFileAsync(presets);
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
