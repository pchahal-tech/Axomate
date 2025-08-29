using System.ComponentModel; // DesignerProperties
using System.Windows;

namespace Axomate.UI.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this)) return;

            // Load the panel only at runtime
            ContentHost.Content = new AdminPanel();
            // (No need to set DataContext here; AdminPanel already resolves its VM at runtime.)
        }
    }
}
