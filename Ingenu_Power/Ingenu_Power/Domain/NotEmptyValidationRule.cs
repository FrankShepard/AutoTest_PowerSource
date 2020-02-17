using System.Globalization;
using System.Windows.Controls;

namespace Ingenu_Power.Domain
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
                ? new ValidationResult(false, "请填写正确值")
                : ValidationResult.ValidResult;
        }
    }
}
