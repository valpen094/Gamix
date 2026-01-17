using System.Windows;
using System.Windows.Controls;

namespace Gamix.UI.Views.Components
{
    public partial class HeaderView : System.Windows.Controls.UserControl
    {
        public event RoutedEventHandler? SidebarRequested;

        public HeaderView()
        {
            InitializeComponent();
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
                var dialog = new ThemeSelectionDialog(viewModel.AvailableThemes, viewModel.CurrentTheme, (theme) => 
                {
                    viewModel.ChangeThemeCommand.Execute(theme);
                });
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }
    }
}
