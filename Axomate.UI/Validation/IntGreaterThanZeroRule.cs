using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class IntGreaterThanZeroRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var s = value?.ToString();
            if (int.TryParse(s, out var i) && i >= 1)
                return ValidationResult.ValidResult;
            return new ValidationResult(false, "Qty must be an integer ≥ 1.");
        }
    }
}
