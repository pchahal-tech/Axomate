using Axomate.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Axomate.UI.Services
{
    public interface IWindowService
    {
        void ShowAdmin();
    }

    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _sp;
        public WindowService(IServiceProvider sp) => _sp = sp;

        public void ShowAdmin()
        {
            var win = _sp.GetRequiredService<Axomate.UI.Views.AdminWindow>();
            win.Owner = Application.Current?.MainWindow;
            win.DataContext = _sp.GetRequiredService<AdminViewModel>();
            win.ShowDialog();
        }
    }
}
