using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using PropertyManager.Application.Auth;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Tests.Auth;

/// <summary>
/// Unit tests for RegisterCommand with invitation token requirement (TDD).
/// Tests written before implementation.
/// </summary>
public class RegisterWithInvitationTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly List<Invitation> _invitations;
    private readonly List<Account> _accounts;
    private readonly Guid _testInvitationId = Guid.NewGuid();
    private readonly Guid _inviterAccountId = Guid.NewGuid();
    private readonly Guid _inviterUserId = Guid.NewGuid();
    private readonly string _validRawToken;
    private readonly string _validTokenHash;

    public RegisterWithInvitationTests()
    {
        // Generate a valid token pair
        _validRawToken = GenerateSecureToken();
        _validTokenHash = HashToken(_validRawToken);

        _invitations = new List<Invitation>();
        _accounts = new List<Account>();

        _dbContextMock = new Mock<IAppDbContext>();
        SetupInvitationsDbSet();
        SetupAccountsDbSet();
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _identityServiceMock.Setup(x => x.CreateUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Guid.NewGuid(), Array.Empty<string>()));

        _emailServiceMock = new Mock<IEmailService>();
    }

    private void SetupInvitationsDbSet()
    {
        var invitationsDbSetMock = _invitations.AsQueryable().BuildMockDbSet();
        invitationsDbSetMock.Setup(m => m.Add(It.IsAny<Invitation>())).Callback<Invitation>(_invitations.Add);
        _dbContextMock.Setup(x => x.Invitations).Returns(invitationsDbSetMock.Object);
    }

    private void SetupAccountsDbSet()
    {
        var accountsDbSetMock = _accounts.AsQueryable().BuildMockDbSet();
        accountsDbSetMock.Setup(m => m.Add(It.IsAny<Account>())).Callback<Account>(_accounts.Add);
        _dbContextMock.Setup(x => x.Accounts).Returns(accountsDbSetMock.Object);
    }

    private void AddValidPendingInvitation(string email = "invited@example.com")
    {
        _invitations.Add(new Invitation
        {
            Id = _testInvitationId,
            AccountId = _inviterAccountId,
            Email = email,
            TokenHash = _validTokenHash,
            InvitedByUserId = _inviterUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            AcceptedAt = null
        });
        SetupInvitationsDbSet();
    }

    #region Validator Tests

    [Fact]
    public void Validator_ValidTokenAndPassword_PassesValidation()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validator_EmptyToken_FailsValidation(string? token)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand("ValidP@ss1", token!);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvitationToken");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nocaps1!")]
    [InlineData("NOLOWER1!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSpecial1")]
    public void Validator_InvalidPassword_FailsValidation(string password)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand(password, _validRawToken);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    #endregion

    #region Handler Tests

    [Fact]
    public async Task Handle_ValidToken_CreatesAccountAndUser()
    {
        // Arrange
        AddValidPendingInvitation("newuser@example.com");
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().NotBe(Guid.Empty);
        _accounts.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ValidToken_DeriveAccountNameFromEmail()
    {
        // Arrange
        AddValidPendingInvitation("dave.smith@example.com");
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _accounts.Should().ContainSingle();
        _accounts[0].Name.Should().Be("dave.smith");
    }

    [Fact]
    public async Task Handle_ValidToken_CreatesUserWithOwnerRole()
    {
        // Arrange
        AddValidPendingInvitation();
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(x => x.CreateUserAsync(
            "invited@example.com",
            "ValidP@ss1",
            It.IsAny<Guid>(),
            "Owner",
            It.IsAny<CancellationToken>(),
            true), // emailConfirmed = true
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_SetsEmailConfirmedTrue()
    {
        // Arrange
        AddValidPendingInvitation();
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - emailConfirmed parameter should be true
        _identityServiceMock.Verify(x => x.CreateUserAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            true),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidToken_DoesNotRequireEmailVerification()
    {
        // Arrange
        AddValidPendingInvitation();
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.RequiresEmailVerification.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidToken_DoesNotSendVerificationEmail()
    {
        // Arrange
        AddValidPendingInvitation();
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidToken_MarksInvitationAsAccepted()
    {
        // Arrange
        AddValidPendingInvitation();
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations[0].AcceptedAt.Should().NotBeNull();
        _invitations[0].IsAccepted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsValidationException()
    {
        // Arrange - add expired invitation
        _invitations.Add(new Invitation
        {
            Id = _testInvitationId,
            AccountId = _inviterAccountId,
            Email = "expired@example.com",
            TokenHash = _validTokenHash,
            InvitedByUserId = _inviterUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            AcceptedAt = null
        });
        SetupInvitationsDbSet();

        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid or expired*");
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsValidationException()
    {
        // Arrange - add already accepted invitation
        _invitations.Add(new Invitation
        {
            Id = _testInvitationId,
            AccountId = _inviterAccountId,
            Email = "used@example.com",
            TokenHash = _validTokenHash,
            InvitedByUserId = _inviterUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            AcceptedAt = DateTime.UtcNow.AddHours(-1) // Already used
        });
        SetupInvitationsDbSet();

        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", _validRawToken);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid or expired*");
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsValidationException()
    {
        // Arrange - no invitation exists for this token
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", "invalid-token-that-does-not-exist");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid or expired*");
    }

    [Fact]
    public async Task Handle_MissingToken_ThrowsValidationException()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new RegisterCommand("ValidP@ss1", "");

        // Act - validation should fail before handler
        var validator = new RegisterCommandValidator();
        var validationResult = await validator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "InvitationToken");
    }

    #endregion

    private RegisterCommandHandler CreateHandler()
    {
        return new RegisterCommandHandler(
            _dbContextMock.Object,
            _identityServiceMock.Object,
            _emailServiceMock.Object);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
