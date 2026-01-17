using System.Windows;
using System.Drawing;
using System.Windows.Input;
using System.Globalization;
using System.IO;
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

            this.Closing += MainWindow_Closing;
            
            // プリセット適用時にサイドバーを自動で閉じる
            viewModel.PresetApplied += () => Sidebar.Hide();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // アプリを終了するのではなく、非表示にする（トレイに隠れる）
            e.Cancel = true;
            this.Hide();
        }

        /// <summary>
        /// ウィンドウのドラッグ移動を処理します。
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 子要素（ボタン等）で既に処理されていたらスキップ
            if (e.Handled) return;

            // Border 自体がクリックされた場合のみドラッグを許可
            // これにより、ボタンやスライダー上でのクリックではドラッグが発動しない
            if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// サイドバーを表示します。
        /// </summary>
        private void HeaderView_SidebarRequested(object sender, RoutedEventArgs e)
        {
            Sidebar.Show();
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
