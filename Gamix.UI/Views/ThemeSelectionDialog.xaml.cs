using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace Gamix.UI.Views
{
    public partial class ThemeSelectionDialog : Window
    {
        public ObservableCollection<ThemeOption> ThemeOptions { get; } = [];
        public string Result { get; private set; } = string.Empty;

        public ICommand SelectThemeCommand { get; }

        private readonly System.Action<string>? _onApplyTheme;

        public ThemeSelectionDialog(IEnumerable<string> availableThemes, string currentTheme, System.Action<string>? onApplyTheme = null)
        {
            InitializeComponent();
            DataContext = this;
            _onApplyTheme = onApplyTheme;

            SelectThemeCommand = new RelayCommand<ThemeOption>(SelectTheme);

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

                // プリロード済みのキャッシュがあればそれを使用する（WPFはURIが同じなら自動的にキャッシュするが、デコード済みであることを確実にする）
                ThemeOptions.Add(option);
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
                        // 絶対パック URI を使用してリソースを正しく解決する
                        var uri = new System.Uri($"pack://application:,,,/Gamix.UI;component/Resources/Images/{theme.ToLower()}_theme_preview.png", System.UriKind.Absolute);
                        // UIスレッドで実行する必要があるため、Application.Current.Dispatcher を使用
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = uri;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // これで即座に読み込み・デコード
                            bitmap.EndInit();
                            bitmap.Freeze(); // スレッドをまたいで利用可能にする
                        });
                    }
                    catch
                    {
                        // 読み込み失敗は無視（実行時に再度試行される）
                    }
                }
            });
        }

        private void SelectTheme(ThemeOption? option)
        {
            if (option == null) return;

            foreach (var item in ThemeOptions)
            {
                item.IsSelected = (item == option);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ThemeOptions.FirstOrDefault(t => t.IsSelected);
            if (selected != null)
            {
                _onApplyTheme?.Invoke(selected.Name);
                // Do not close the dialog so the user can see the effect
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
