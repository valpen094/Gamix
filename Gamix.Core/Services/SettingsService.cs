using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gamix.Core.Services
{
    /// <summary>
    /// JSON ファイルを使用した ISettingsService の実装。
    /// 設定は %AppData%/Gamix/settings.json に保存されます。
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly string _filePath;
        private AppSettings _settings;

        /// <summary>
        /// SettingsService のコンストラクタ。
        /// </summary>
        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "Gamix");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "settings.json");
            _settings = LoadSettings();
        }

        /// <inheritdoc/>
        public Task<string?> GetSelectedOutputDeviceIdAsync()
        {
            return Task.FromResult(_settings.SelectedOutputDeviceId);
        }

        /// <inheritdoc/>
        public Task<string?> GetSelectedInputDeviceIdAsync()
        {
            return Task.FromResult(_settings.SelectedInputDeviceId);
        }

        /// <inheritdoc/>
        public async Task SetSelectedOutputDeviceIdAsync(string deviceId)
        {
            _settings.SelectedOutputDeviceId = deviceId;
            await SaveSettingsAsync();
        }

        /// <inheritdoc/>
        public async Task SetSelectedInputDeviceIdAsync(string deviceId)
        {
            _settings.SelectedInputDeviceId = deviceId;
            await SaveSettingsAsync();
        }

        /// <inheritdoc/>
        public Task<string?> GetThemeAsync()
        {
            return Task.FromResult(_settings.Theme);
        }

        /// <inheritdoc/>
        public async Task SetThemeAsync(string themeName)
        {
            _settings.Theme = themeName;
            await SaveSettingsAsync();
        }

        /// <summary>
        /// 設定をファイルから読み込みます。
        /// </summary>
        private AppSettings LoadSettings()
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }

        /// <summary>
        /// アプリケーション設定を保持する内部クラス。
        /// </summary>
        private class AppSettings
        {
            public string? SelectedOutputDeviceId { get; set; }
            public string? SelectedInputDeviceId { get; set; }
            public string? Theme { get; set; } = "Pastel";
        }
    }
}
