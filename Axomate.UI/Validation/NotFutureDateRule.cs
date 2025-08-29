using System.Globalization;
using System.Windows.Controls;

namespace Axomate.UI.Validation
{
    public class NotFutureDateRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is null) return ValidationResult.ValidResult;

            DateTime dt;
            if (value is DateTime d) dt = d;
            else if (!DateTime.TryParse(value.ToString(), out dt))
                return new ValidationResult(false, "Invalid date.");

            if (dt.Date > DateTime.Today)
                return new ValidationResult(false, "Service date cannot be in the future.");

            return ValidationResult.ValidResult;
        }
    }
}
