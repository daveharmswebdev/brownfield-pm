using FluentAssertions;
using PropertyManager.Application.Properties;

namespace PropertyManager.Application.Tests.Properties;

/// <summary>
/// Unit tests for CreatePropertyCommandValidator (AC-2.1.5).
/// </summary>
public class CreatePropertyValidatorTests
{
    private readonly CreatePropertyCommandValidator _validator;

    public CreatePropertyValidatorTests()
    {
        _validator = new CreatePropertyCommandValidator();
    }

    [Fact]
    public async Task Validate_ValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_MissingName_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required");
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: new string('A', 256),
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name must be 255 characters or less");
    }

    [Fact]
    public async Task Validate_MissingStreet_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "",
            City: "Austin",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Street" && e.ErrorMessage == "Street is required");
    }

    [Fact]
    public async Task Validate_MissingCity_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "",
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City" && e.ErrorMessage == "City is required");
    }

    [Fact]
    public async Task Validate_CityTooLong_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: new string('A', 101),
            State: "TX",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City" && e.ErrorMessage == "City must be 100 characters or less");
    }

    [Fact]
    public async Task Validate_MissingState_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "",
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Theory]
    [InlineData("T")]
    [InlineData("TEX")]
    [InlineData("Texas")]
    public async Task Validate_InvalidStateLength_ReturnsValidationError(string state)
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: state,
            ZipCode: "78701");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State" && e.ErrorMessage == "State must be exactly 2 characters");
    }

    [Fact]
    public async Task Validate_MissingZipCode_ReturnsValidationError()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ZipCode");
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("ABCDE")]
    [InlineData("1234A")]
    [InlineData("12-45")]
    public async Task Validate_InvalidZipCodeFormat_ReturnsValidationError(string zipCode)
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "Oak Street Duplex",
            Street: "123 Oak Street",
            City: "Austin",
            State: "TX",
            ZipCode: zipCode);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ZipCode" && e.ErrorMessage == "ZIP Code must be exactly 5 digits");
    }

    [Fact]
    public async Task Validate_AllFieldsMissing_ReturnsMultipleErrors()
    {
        // Arrange
        var command = new CreatePropertyCommand(
            Name: "",
            Street: "",
            City: "",
            State: "",
            ZipCode: "");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
    }
}
