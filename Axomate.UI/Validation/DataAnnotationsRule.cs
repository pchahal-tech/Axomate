using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Controls;
using DataAnnotationsValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
// aliases
using WpfValidationResult = System.Windows.Controls.ValidationResult;

namespace Axomate.UI.Validation
{
    public class DataAnnotationsRule : ValidationRule
    {
        public Type? SourceType { get; set; }
        public string? PropertyName { get; set; }

        public override WpfValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (SourceType == null || string.IsNullOrWhiteSpace(PropertyName))
                return WpfValidationResult.ValidResult;

            var prop = SourceType.GetProperty(PropertyName);
            if (prop == null) return WpfValidationResult.ValidResult;

            var attributes = prop
                .GetCustomAttributes(typeof(ValidationAttribute), true)
                .Cast<ValidationAttribute>()
                .ToArray();

            // empty value is OK unless [Required]
            if ((value == null || string.IsNullOrWhiteSpace(value.ToString())) &&
                !attributes.Any(a => a is RequiredAttribute))
                return WpfValidationResult.ValidResult;

            var ctx = new DataAnnotationsValidationContext(new object()) { MemberName = PropertyName };

            foreach (var attr in attributes)
            {
                var result = attr.GetValidationResult(value, ctx);
                if (result != DataAnnotationsValidationResult.Success)
                    return new WpfValidationResult(false, result?.ErrorMessage ?? "Invalid value");
            }

            return WpfValidationResult.ValidResult;
        }
    }
}
