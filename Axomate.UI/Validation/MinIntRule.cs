using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class MinIntRule : ValidationRule
    {
        public int Min { get; set; } = 0;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var text = value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text)) return ValidationResult.ValidResult;
            if (!int.TryParse(text, out var n))
                return new ValidationResult(false, "Must be a whole number.");
            if (n < Min)
                return new ValidationResult(false, $"Must be ≥ {Min}.");
            return ValidationResult.ValidResult;
        }
    }
}