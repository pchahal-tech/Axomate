using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Axomate.UI
{
    public partial class SplashScreenWindow : Window
    {
        public event Action? CancelRequested;

        public SplashScreenWindow()
        {
            InitializeComponent();

            // allow ESC key to cancel
            this.KeyDown += SplashScreenWindow_KeyDown;
            this.Focusable = true;
            this.Focus(); // ensure window has keyboard focus
        }

        private void SplashScreenWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelRequested?.Invoke();
            }
        }

        public void SetStatus(string message)
        {
            if (!CheckAccess()) { Dispatcher.Invoke(() => SetStatus(message)); return; }
            StatusText.Text = message ?? string.Empty;
        }

        public void SetProgress(double percent)
        {
            if (!CheckAccess()) { Dispatcher.Invoke(() => SetProgress(percent)); return; }
            ProgressBarControl.IsIndeterminate = false;
            var p = Math.Max(0, Math.Min(100, percent));
            ProgressBarControl.Value = p;
        }

        public void SetIndeterminate(bool on)
        {
            if (!CheckAccess()) { Dispatcher.Invoke(() => SetIndeterminate(on)); return; }
            ProgressBarControl.IsIndeterminate = on;
        }

        public void SetFooter(string text)
        {
            if (!CheckAccess()) { Dispatcher.Invoke(() => SetFooter(text)); return; }
            FooterText.Text = text ?? string.Empty;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke();
        }
    }
}
