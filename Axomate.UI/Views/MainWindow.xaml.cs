using Axomate.UI.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Axomate.UI.Views
{
    public partial class MainWindow : Window
    {
        private static readonly Regex DigitsOnly = new(@"^[0-9]+$");
        private bool _suppressSelectionHandler = false;
        private int _lastSelectedIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => _lastSelectedIndex = MainTabs.SelectedIndex;
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _suppressSelectionHandler) return;

            // If user switched to Admin while locked, defer the dialog until dispatcher is idle
            if (AdminTab?.IsSelected == true && DataContext is MainViewModel vm && !vm.IsAdminUnlocked)
            {
                var previousIndex = _lastSelectedIndex;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    vm.UnlockAdminCommand.Execute(null);

                    if (!vm.IsAdminUnlocked)
                    {
                        // bounce back without re-triggering handler
                        _suppressSelectionHandler = true;
                        try
                        {
                            MainTabs.SelectedIndex = previousIndex;
                        }
                        finally
                        {
                            _suppressSelectionHandler = false;
                        }
                    }
                    else
                    {
                        _lastSelectedIndex = MainTabs.SelectedIndex;
                    }
                }), DispatcherPriority.Background);

                // stop here; we'll update _lastSelectedIndex in the deferred block
                return;
            }

            _lastSelectedIndex = MainTabs.SelectedIndex;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Opacity > 0.99)
            {
                e.Cancel = true;
                var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                anim.Completed += (_, __) => Close(); // re-close without cancel
                BeginAnimation(OpacityProperty, anim);
            }
            else
            {
                base.OnClosing(e);
            }
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var proposed = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                  .Insert(tb.SelectionStart, e.Text);
            e.Handled = proposed.Length > 0 && !DigitsOnly.IsMatch(proposed);
        }

        private void NumericOnly_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var tb = (TextBox)sender;
            var pasteText = (string?)e.DataObject.GetData(DataFormats.Text) ?? "";
            var proposed = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                  .Insert(tb.SelectionStart, pasteText);

            if (proposed.Length > 0 && !DigitsOnly.IsMatch(proposed))
                e.CancelCommand();
        }

        private void Qty_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && (!int.TryParse(tb.Text, out var qty) || qty < 1))
                tb.Text = "1";
        }

        // Handles BOTH: LostFocus and LostKeyboardFocus on the mileage TextBox
        private void Mileage_LostFocusCommit(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

    }
}
