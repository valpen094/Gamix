using System.Windows;

namespace Gamix.UI.Views
{
    /// <summary>
    /// カスタムスタイルの入力ダイアログ。
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// ユーザーが入力した結果。
        /// </summary>
        public string? Result { get; private set; }

        /// <summary>
        /// InputDialog のコンストラクタ。
        /// </summary>
        /// <param name="defaultValue">入力欄の初期値。</param>
        public InputDialog(string? defaultValue = null)
        {
            InitializeComponent();
            InputTextBox.Text = defaultValue ?? string.Empty;
            InputTextBox.SelectAll();
            InputTextBox.Focus();
            
            UpdateOkButtonState();
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateOkButtonState();
        }

        private void UpdateOkButtonState()
        {
            if (OkBtn != null)
            {
                OkBtn.IsEnabled = !string.IsNullOrWhiteSpace(InputTextBox.Text);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            DialogResult = false;
            Close();
        }
    }
}
