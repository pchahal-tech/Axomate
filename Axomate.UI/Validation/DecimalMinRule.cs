using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class DecimalMinRule : ValidationRule
    {
        public decimal Min { get; set; } = 0m;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var text = value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text)) return ValidationResult.ValidResult;
            if (!decimal.TryParse(text, out var d))
                return new ValidationResult(false, "Must be a number.");
            if (d < Min)
                return new ValidationResult(false, $"Must be ≥ {Min}.");
            return ValidationResult.ValidResult;
        }
    }
}
