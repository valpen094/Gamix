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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void ShowSidebar_Click(object sender, RoutedEventArgs e)
        {
            SidebarRequested?.Invoke(this, e);
        }

        private void PaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Resources["ThemeMenu"] is System.Windows.Controls.ContextMenu menu)
            {
                // Ensure DataContext is passed down since it's not in the visual tree
                menu.DataContext = button.DataContext;
                menu.PlacementTarget = button;
                menu.IsOpen = true;
            }
        }
    }
}
