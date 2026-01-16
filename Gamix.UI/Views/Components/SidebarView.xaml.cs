using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Gamix.UI.ViewModels;
using Gamix.Core.Models;
using Microsoft.VisualBasic;

namespace Gamix.UI.Views.Components
{
    public partial class SidebarView : UserControl
    {
        public SidebarView()
        {
            InitializeComponent();
        }

        public void Show()
        {
            (FindResource("ShowSidebar") as Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = true;
        }

        public void Hide()
        {
            (FindResource("HideSidebar") as Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = false;
        }

        private void HideSidebar_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Hide();
        }

        private async void RenamePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Preset preset)
            {
                var newName = Interaction.InputBox(
                    "新しいプリセット名を入力してください:",
                    "名前変更",
                    preset.Name);
                
                if (!string.IsNullOrWhiteSpace(newName) && newName != preset.Name)
                {
                    var vm = DataContext as MainViewModel;
                    if (vm?.RenamePresetCommand.CanExecute((preset, newName)) == true)
                    {
                        await vm.RenamePresetCommand.ExecuteAsync((preset, newName));
                    }
                }
            }
        }
    }
}
