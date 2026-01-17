using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Gamix.UI.Views
{
    public partial class ThemeSelectionDialog : Window
    {
        public ObservableCollection<ThemeOption> ThemeOptions { get; } = new ObservableCollection<ThemeOption>();
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
                
                ThemeOptions.Add(new ThemeOption
                {
                    Name = theme,
                    ImagePath = imagePath,
                    IsSelected = isSelected
                });
            }
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
