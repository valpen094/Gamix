using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gamix.Core.Utils;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Gamix.UI.Views.Components
{
    public partial class HeaderView : System.Windows.Controls.UserControl
    {
        public event RoutedEventHandler? SidebarRequested;
        private const int MaxVisualChars = 5;
        private bool _isUpdating = false;

        public HeaderView()
        {
            InitializeComponent();
            PresetNameTextBox.LostFocus += PresetNameTextBox_LostFocus;
            PresetNameTextBox.PreviewKeyDown += PresetNameTextBox_PreviewKeyDown;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void ShowSidebar_Click(object sender, RoutedEventArgs e)
        {
            SidebarRequested?.Invoke(this, e);
        }

        private void PaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                // Pass a callback to apply the theme immediately without closing the dialog
                var selectionViewModel = new Gamix.UI.ViewModels.ThemeSelectionViewModel(viewModel.Themes.AvailableThemes, viewModel.Themes.CurrentTheme, (theme) => 
                {
                    viewModel.Themes.ChangeThemeCommand.Execute(theme);
                });
                
                var dialog = new ThemeSelectionDialog(selectionViewModel);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

        private void PresetNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sender is not TextBox textBox) return;

            // IME変換中はスキップ（変換確定後にLostFocusまたはEnterで処理）
            if (InputMethod.Current?.ImeState == InputMethodState.On) return;

            EnforceVisualLimit(textBox);
        }

        private void PresetNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                EnforceVisualLimit(textBox);
            }
        }

        private void PresetNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enterキーでも確定時に制限を適用
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                EnforceVisualLimit(textBox);
            }
        }

        private void EnforceVisualLimit(TextBox textBox)
        {
            if (_isUpdating) return;

            var text = textBox.Text ?? string.Empty;
            if (text.GetVisualLength() > MaxVisualChars)
            {
                _isUpdating = true;
                var truncated = text.TruncateVisual(MaxVisualChars);
                var caretPos = textBox.CaretIndex;
                textBox.Text = truncated;
                textBox.CaretIndex = Math.Min(caretPos, truncated.Length);
                _isUpdating = false;
            }
        }
    }
}
