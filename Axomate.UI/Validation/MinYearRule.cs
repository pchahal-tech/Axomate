using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class MinYearRule : ValidationRule
    {
        public int Min { get; set; } = 1900;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var text = value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return ValidationResult.ValidResult;

            if (!int.TryParse(text, out var year))
                return new ValidationResult(false, "Year must be numeric.");

            if (year < Min)
                return new ValidationResult(false, $"Year cannot be before {Min}.");

            return ValidationResult.ValidResult;
        }
    }
}
