using System.Text.RegularExpressions;
using FluentValidation;

namespace PropertyManager.Application.Expenses;

/// <summary>
/// Validator for CreateExpenseCommand (AC-3.1.2, AC-3.1.3, AC-3.1.5).
/// </summary>
public partial class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property is required");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than $0")
            .LessThanOrEqualTo(9999999.99m).WithMessage("Amount exceeds maximum of $9,999,999.99")
            .PrecisionScale(10, 2, true).WithMessage("Amount can have at most 2 decimal places");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date cannot be in the future");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be 500 characters or less")
            .Must(NotContainHtml).WithMessage("Description cannot contain HTML");
    }

    private static bool NotContainHtml(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return true;

        // Check for HTML tags
        return !HtmlTagRegex().IsMatch(description);
    }

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
