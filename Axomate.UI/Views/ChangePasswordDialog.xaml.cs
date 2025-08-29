using System.Windows;

namespace Axomate.UI.Views
{
    public partial class ChangePasswordDialog : Window
    {
        public string? NewPassword { get; private set; }
        public ChangePasswordDialog() => InitializeComponent();

        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Pwd1.Password))
            {
                ShowError("Password cannot be empty.");
                return;
            }
            if (Pwd1.Password != Pwd2.Password)
            {
                ShowError("Passwords do not match.");
                return;
            }
            NewPassword = Pwd1.Password;
            DialogResult = true;
            Close();
        }

        private void ShowError(string msg)
        {
            Error.Text = msg;
            Error.Visibility = Visibility.Visible;
        }
    }
}
