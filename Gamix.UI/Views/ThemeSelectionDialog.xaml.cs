using Gamix.UI.ViewModels;
using System.Windows;

namespace Gamix.UI.Views
{
    public partial class ThemeSelectionDialog : Window
    {
        public ThemeSelectionDialog(ThemeSelectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
