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
                var selectionViewModel = new Gamix.UI.ViewModels.ThemeSelectionViewModel(viewModel.Themes.AvailableThemes, viewModel.Themes.CurrentTheme, (theme) => 
                {
                    viewModel.Themes.ChangeThemeCommand.Execute(theme);
                });
                
                var dialog = new ThemeSelectionDialog(selectionViewModel);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }
    }
}
