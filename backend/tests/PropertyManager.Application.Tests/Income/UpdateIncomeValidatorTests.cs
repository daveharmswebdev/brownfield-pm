using FluentAssertions;
using PropertyManager.Application.Income;

namespace PropertyManager.Application.Tests.Income;

/// <summary>
/// Unit tests for UpdateIncomeValidator (AC-4.2.4).
/// </summary>
public class UpdateIncomeValidatorTests
{
    private readonly UpdateIncomeValidator _validator;

    public UpdateIncomeValidatorTests()
    {
        _validator = new UpdateIncomeValidator();
    }

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: "January rent");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_EmptyId_FailsValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.Empty,
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task Validate_ZeroAmount_FailsValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 0m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Amount" &&
            e.ErrorMessage.Contains("greater than"));
    }

    [Fact]
    public async Task Validate_NegativeAmount_FailsValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: -100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "John Smith",
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Amount" &&
            e.ErrorMessage.Contains("greater than"));
    }

    [Fact]
    public async Task Validate_SmallPositiveAmount_PassesValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 0.01m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DefaultDate_FailsValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: default,
            Source: "John Smith",
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    [Fact]
    public async Task Validate_NullSourceAndDescription_PassesValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 1500.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: null,
            Description: null);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_LargeAmount_PassesValidation()
    {
        // Arrange
        var command = new UpdateIncomeCommand(
            Id: Guid.NewGuid(),
            Amount: 99999999.99m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            Source: "Large payment",
            Description: "Annual lease payment");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
