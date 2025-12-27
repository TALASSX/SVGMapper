using System.Windows;

namespace SVGMapper.Minimal
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; set; } = string.Empty;

        public InputDialog(string prompt, string defaultText = "")
        {
            InitializeComponent();
            PromptText.Text = prompt;
            ResponseBox.Text = defaultText;
            ResponseBox.Focus();
            ResponseBox.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = ResponseBox.Text ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}