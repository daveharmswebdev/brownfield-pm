using FluentAssertions;
using FluentValidation.TestHelper;
using PropertyManager.Application.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for CreateIncomeValidator (AC-4.1.5).
/// </summary>
public class CreateIncomeValidatorTests
{
    private readonly CreateIncomeValidator _validator;

    public CreateIncomeValidatorTests()
    {
        _validator = new CreateIncomeValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: "January rent payment");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPropertyId_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.Empty,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PropertyId)
            .WithErrorMessage("Property is required");
    }

    [Fact]
    public void Validate_ZeroAmount_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 0,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than $0");
    }

    [Fact]
    public void Validate_NegativeAmount_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: -100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than $0");
    }

    [Fact]
    public void Validate_AmountExceedsMax_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 10000000.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount exceeds maximum of $9,999,999.99");
    }

    [Fact]
    public void Validate_EmptyDate_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: default,
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorMessage("Date is required");
    }

    [Fact]
    public void Validate_SourceExceedsMax_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: new string('A', 256),
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Source)
            .WithErrorMessage("Source must be 255 characters or less");
    }

    [Fact]
    public void Validate_SourceWithHtml_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "<script>alert('xss')</script>",
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Source)
            .WithErrorMessage("Source cannot contain HTML");
    }

    [Fact]
    public void Validate_DescriptionExceedsMax_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: new string('A', 501));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must be 500 characters or less");
    }

    [Fact]
    public void Validate_DescriptionWithHtml_ShouldFail()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: "<script>alert('xss')</script>");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot contain HTML");
    }

    [Fact]
    public void Validate_NullSourceAndDescription_ShouldPass()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MinimumValidAmount_ShouldPass()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 0.01m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MaxValidAmount_ShouldPass()
    {
        // Arrange
        var command = new CreateIncomeCommand(
            PropertyId: Guid.NewGuid(),
            Amount: 9999999.99m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
