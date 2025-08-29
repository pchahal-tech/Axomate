using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Axomate.UI.Behaviors
{
    public static class NumericBehavior
    {
        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached(
                "IsNumericOnly",
                typeof(bool),
                typeof(NumericBehavior),
                new UIPropertyMetadata(false, OnIsNumericOnlyChanged));

        public static bool GetIsNumericOnly(DependencyObject obj) => (bool)obj.GetValue(IsNumericOnlyProperty);
        public static void SetIsNumericOnly(DependencyObject obj, bool value) => obj.SetValue(IsNumericOnlyProperty, value);

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += OnPreviewTextInput;
                    DataObject.AddPastingHandler(tb, OnPaste);
                }
                else
                {
                    tb.PreviewTextInput -= OnPreviewTextInput;
                    DataObject.RemovePastingHandler(tb, OnPaste);
                }
            }
        }

        private static readonly Regex Digits = new(@"^[0-9]+$");

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextValid((TextBox)sender, e.Text);
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var text = e.DataObject.GetData(DataFormats.Text) as string ?? "";
                if (!IsTextValid((TextBox)sender, text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private static bool IsTextValid(TextBox tb, string newText)
        {
            // Proposed text after this input/paste
            var proposed = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                  .Insert(tb.SelectionStart, newText);

            if (string.IsNullOrEmpty(proposed)) return true;        // allow clearing
            if (!Digits.IsMatch(proposed)) return false;            // only digits
            return true;                                            // length cap is via MaxLength in XAML
        }
    }
}
