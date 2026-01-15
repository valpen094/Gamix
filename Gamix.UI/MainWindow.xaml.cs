using System.Windows;
using System.Windows.Input;
using Gamix.UI.ViewModels;

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
    }
}


