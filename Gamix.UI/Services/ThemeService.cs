using System;
using System.Linq;
using System.Windows;

namespace Gamix.UI.Services
{
    public class ThemeService : IThemeService
    {
        public string CurrentTheme { get; private set; } = string.Empty;

        public void SetTheme(string themeName)
        {
            var uri = new Uri($"pack://application:,,,/Gamix.UI;component/Resources/Themes/{themeName}Theme.xaml", UriKind.Absolute);
            
            try
            {
                var dict = new ResourceDictionary { Source = uri };

                var oldDict = Application.Current.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));

                if (oldDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                }

                Application.Current.Resources.MergedDictionaries.Add(dict);
                CurrentTheme = themeName;
            }
            catch (Exception ex)
            {
                // In a real app, log this
                System.Diagnostics.Debug.WriteLine($"Failed to load theme {themeName}: {ex.Message}");
            }
        }
    }
}
