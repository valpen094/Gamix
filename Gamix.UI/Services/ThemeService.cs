using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Gamix.Core.Services;

namespace Gamix.UI.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ISettingsService _settingsService;
        public string CurrentTheme { get; private set; } = string.Empty;

        public ThemeService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            InitializeTheme();
        }

        private async void InitializeTheme()
        {
            var savedTheme = await _settingsService.GetThemeAsync();
            SetTheme(savedTheme ?? "Pastel");
        }

        public async void SetTheme(string themeName)
        {
            var uri = new Uri($"pack://application:,,,/Gamix.UI;component/Resources/Themes/{themeName}Theme.xaml", UriKind.Absolute);
            
            try
            {
                var dict = new ResourceDictionary { Source = uri };

                var oldDict = System.Windows.Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));

                if (oldDict != null)
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
                CurrentTheme = themeName;

                // Save setting (avoid overwriting on init if same)
                var currentSaved = await _settingsService.GetThemeAsync();
                if (currentSaved != themeName)
                {
                    await _settingsService.SetThemeAsync(themeName);
                }
            }
            catch (Exception ex)
            {
                // In a real app, log this
                System.Diagnostics.Debug.WriteLine($"Failed to load theme {themeName}: {ex.Message}");
            }
        }

        public IEnumerable<string> GetAvailableThemes()
        {
            // In a real application, you might scan the directory or resources.
            // For now, we return our known themes.
            return new List<string>
            {
                "Pastel",
                "Dark",
                "Light",
                "Gaming"
            };
        }
    }
}
