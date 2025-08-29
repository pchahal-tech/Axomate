using Axomate.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel; // DesignerProperties
using System.Windows.Controls;

namespace Axomate.UI.Views
{
    public partial class AdminPanel : UserControl
    {
        public AdminPanel()
        {
            InitializeComponent();

            // Don’t resolve DI while the designer renders
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            DataContext = App.Services.GetRequiredService<AdminViewModel>();
        }
    }
}
