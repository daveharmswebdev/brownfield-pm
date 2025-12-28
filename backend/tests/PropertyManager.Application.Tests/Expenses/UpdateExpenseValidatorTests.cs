using FluentAssertions;
using PropertyManager.Application.Expenses;

namespace PropertyManager.Application.Tests.Expenses;

/// <summary>
/// Unit tests for UpdateExpenseValidator (AC-3.2.1).
/// </summary>
public class UpdateExpenseValidatorTests
{
    private readonly UpdateExpenseValidator _validator;
    private readonly Guid _validId = Guid.NewGuid();
    private readonly Guid _validCategoryId = Guid.NewGuid();

    public UpdateExpenseValidatorTests()
    {
        _validator = new UpdateExpenseValidator();
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Valid description");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: Guid.Empty,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_EmptyCategoryId_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: Guid.Empty,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void Validate_NegativeAmount_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: -50.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("greater than"));
    }

    [Fact]
    public void Validate_ZeroAmount_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 0m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void Validate_AmountExceedsMaximum_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 10000000.00m, // Exceeds 9,999,999.99
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("maximum"));
    }

    [Fact]
    public void Validate_AmountTooManyDecimalPlaces_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.999m, // More than 2 decimal places
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount" && e.ErrorMessage.Contains("decimal"));
    }

    [Fact]
    public void Validate_FutureDate_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date" && e.ErrorMessage.Contains("future"));
    }

    [Fact]
    public void Validate_TodayDate_Passes()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PastDate_Passes()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            CategoryId: _validCategoryId,
            Description: "Test");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        // Arrange
        var longDescription = new string('a', 501);
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: longDescription);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("500"));
    }

    [Fact]
    public void Validate_DescriptionExactlyMaxLength_Passes()
    {
        // Arrange
        var maxDescription = new string('a', 500);
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: maxDescription);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DescriptionContainsHtml_Fails()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "Test <script>alert('xss')</script>");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("HTML"));
    }

    [Fact]
    public void Validate_NullDescription_Passes()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyDescription_Passes()
    {
        // Arrange
        var command = new UpdateExpenseCommand(
            Id: _validId,
            Amount: 100.00m,
            Date: DateOnly.FromDateTime(DateTime.Today),
            CategoryId: _validCategoryId,
            Description: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
