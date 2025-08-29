using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class NotFutureYearRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            // Allow empty; [Required] (if you add it) can handle mandatory input.
            var text = value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return ValidationResult.ValidResult;

            if (!int.TryParse(text, out var year))
                return new ValidationResult(false, "Year must be numeric.");

            var currentYear = DateTime.Now.Year;
            if (year > currentYear)
                return new ValidationResult(false, $"Year cannot be in the future (>{currentYear}).");

            return ValidationResult.ValidResult;
        }
    }
}
