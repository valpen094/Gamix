using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamix.UI.Services;
using System.Collections.ObjectModel;

namespace Gamix.UI.ViewModels
{
    public partial class ThemeViewModel : ObservableObject
    {
        private readonly IThemeService _themeService;

        public ObservableCollection<string> AvailableThemes { get; } = [];

        public string CurrentTheme => _themeService.CurrentTheme;

        public ThemeViewModel(IThemeService themeService)
        {
            _themeService = themeService;
            LoadThemes();
        }

        private void LoadThemes()
        {
            AvailableThemes.Clear();
            foreach (var theme in _themeService.GetAvailableThemes())
            {
                AvailableThemes.Add(theme);
            }
        }

        [RelayCommand]
        public void ChangeTheme(string themeName)
        {
            if (!string.IsNullOrEmpty(themeName))
            {
                _themeService.SetTheme(themeName);
                OnPropertyChanged(nameof(CurrentTheme));
            }
        }
    }
}
