using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Gamix.UI.ViewModels;
using Gamix.Core.Models;


namespace Gamix.UI.Views.Components
{
    public partial class SidebarView : System.Windows.Controls.UserControl
    {
        public SidebarView()
        {
            InitializeComponent();
        }

        private bool _isVisible = false;

        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            (FindResource("ShowSidebar") as Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = true;
        }

        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;

            (FindResource("HideSidebar") as Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = false;
        }

        private void HideSidebar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Hide();
        }

        private async void RenamePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Preset preset)
            {
                var dialog = new Gamix.UI.Views.InputDialog(preset.Name);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Result) && dialog.Result != preset.Name)
                {
                    var vm = DataContext as MainViewModel;
                    if (vm?.RenamePresetCommand.CanExecute((preset, dialog.Result)) == true)
                    {
                        await vm.RenamePresetCommand.ExecuteAsync((preset, dialog.Result));
                    }
                }
            }
        }
    }
}
