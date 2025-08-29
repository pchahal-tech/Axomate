using Axomate.ApplicationLayer.Interfaces.Services;
using System.Windows;

namespace Axomate.UI.Views
{
    public partial class AdminPasswordDialog : Window
    {
        private readonly IAuthService _auth;
        public bool IsAuthenticated { get; private set; }

        public AdminPasswordDialog(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        private async void OnUnlock(object sender, RoutedEventArgs e)
        {
            var ok = await _auth.VerifyAdminPasswordAsync(Pwd.Password);
            if (ok)
            {
                IsAuthenticated = true;
                DialogResult = true;
                Close();
            }
            else
            {
                Error.Text = "Invalid password.";
                Error.Visibility = Visibility.Visible;
                Pwd.Clear();
                Pwd.Focus();
            }
        }
    }
}
