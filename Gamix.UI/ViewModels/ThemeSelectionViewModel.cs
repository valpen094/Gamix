using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Gamix.UI.ViewModels
{
    public partial class ThemeSelectionViewModel : ObservableObject
    {
        public ObservableCollection<ThemeOption> ThemeOptions { get; } = new();

        private readonly Action<string>? _onApplyTheme;

        public ThemeSelectionViewModel(IEnumerable<string> availableThemes, string currentTheme, Action<string>? onApplyTheme = null)
        {
            _onApplyTheme = onApplyTheme;

            foreach (var theme in availableThemes)
            {
                var isSelected = theme == currentTheme;
                var imagePath = $"/Gamix.UI;component/Resources/Images/{theme.ToLower()}_theme_preview.png";

                var option = new ThemeOption
                {
                    Name = theme,
                    ImagePath = imagePath,
                    IsSelected = isSelected
                };

                ThemeOptions.Add(option);
            }
        }

        [RelayCommand]
        private void SelectTheme(ThemeOption? option)
        {
            if (option == null) return;

            foreach (var item in ThemeOptions)
            {
                item.IsSelected = (item == option);
            }
        }

        [RelayCommand]
        private void ApplyTheme()
        {
            var selected = ThemeOptions.FirstOrDefault(t => t.IsSelected);
            if (selected != null)
            {
                _onApplyTheme?.Invoke(selected.Name);
            }
        }

        /// <summary>
        /// テーマ画像をあらかじめメモリに読み込み、デコードを完了させます。
        /// </summary>
        public static async Task PreloadImagesAsync(IEnumerable<string> availableThemes)
        {
            await Task.Run(() =>
            {
                foreach (var theme in availableThemes)
                {
                    try
                    {
                        var uri = new Uri($"pack://application:,,,/Gamix.UI;component/Resources/Images/{theme.ToLower()}_theme_preview.png", UriKind.Absolute);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = uri;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        });
                    }
                    catch
                    {
                        // 読み込み失敗は無視
                    }
                }
            });
        }
    }

    public partial class ThemeOption : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isSelected;
    }
}
