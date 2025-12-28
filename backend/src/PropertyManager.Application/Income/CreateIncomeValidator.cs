using System.Text.RegularExpressions;
using FluentValidation;

namespace PropertyManager.Application.Income;

/// <summary>
/// Validator for CreateIncomeCommand (AC-4.1.5).
/// </summary>
public partial class CreateIncomeValidator : AbstractValidator<CreateIncomeCommand>
{
    public CreateIncomeValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than $0")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Amount exceeds maximum of $9,999,999.99")
            .PrecisionScale(10, 2, true).WithMessage("Amount can have at most 2 decimal places");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required");

        RuleFor(x => x.Source)
            .MaximumLength(255).WithMessage("Source must be 255 characters or less")
            .Must(NotContainHtml).WithMessage("Source cannot contain HTML");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less")
            .Must(NotContainHtml).WithMessage("Description cannot contain HTML");
    }

    private static bool NotContainHtml(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return !HtmlTagRegex().IsMatch(value);
    }

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
