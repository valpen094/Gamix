using System.Windows;
using System.Windows.Input;
using Gamix.Core.Utils;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Gamix.UI.Views
{
    /// <summary>
    /// カスタムスタイルの入力ダイアログ。
    /// </summary>
    public partial class InputDialog : Window
    {
        private const int MaxVisualChars = 5;
        private bool _isUpdating = false;

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
            InputTextBox.LostFocus += InputTextBox_LostFocus;
            InputTextBox.PreviewKeyDown += InputTextBox_PreviewKeyDown;
            
            UpdateOkButtonState();
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            // IME変換中はスキップ（変換確定後にLostFocusまたはEnterで処理）
            if (InputMethod.Current?.ImeState == InputMethodState.On) return;

            EnforceVisualLimit();
            UpdateOkButtonState();
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            EnforceVisualLimit();
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enterキーでも確定時に制限を適用
            if (e.Key == Key.Enter)
            {
                EnforceVisualLimit();
            }
        }

        private void EnforceVisualLimit()
        {
            if (_isUpdating) return;

            var text = InputTextBox.Text ?? string.Empty;
            if (text.GetVisualLength() > MaxVisualChars)
            {
                _isUpdating = true;
                var truncated = text.TruncateVisual(MaxVisualChars);
                var caretPos = InputTextBox.CaretIndex;
                InputTextBox.Text = truncated;
                InputTextBox.CaretIndex = Math.Min(caretPos, truncated.Length);
                _isUpdating = false;
            }
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
            EnforceVisualLimit(); // OK押下時にも強制
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
