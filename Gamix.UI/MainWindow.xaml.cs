using System.Windows;
using System.Windows.Input;
using System.Globalization;
using Gamix.UI.ViewModels;
using Gamix.Core.Models;

namespace Gamix.UI
{
    /// <summary>
    /// Gamix アプリケーションのメインウィンドウ。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// MainWindow のコンストラクタ。
        /// </summary>
        /// <param name="viewModel">バインドする ViewModel。</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// ウィンドウのドラッグ移動を処理します。
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// ウィンドウを最小化します。
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// プリセットの名前変更ダイアログを表示します。
        /// </summary>
        private async void RenamePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is Preset preset)
            {
                var newName = Microsoft.VisualBasic.Interaction.InputBox(
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


        /// <summary>
        /// サイドバーを表示します。
        /// </summary>
        private void ShowSidebar_Click(object sender, RoutedEventArgs e)
        {
            (FindResource("ShowSidebar") as System.Windows.Media.Animation.Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = true;
        }

        /// <summary>
        /// サイドバーを非表示にします。
        /// </summary>
        private void HideSidebar_Click(object sender, RoutedEventArgs e)
        {
            (FindResource("HideSidebar") as System.Windows.Media.Animation.Storyboard)?.Begin();
            DimOverlay.IsHitTestVisible = false;
        }
    }

    /// <summary>
    /// bool を ❤️ / 🤍 に変換するコンバーター。
    /// </summary>
    public class BoolToHeartConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? "❤️" : "🤍";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
