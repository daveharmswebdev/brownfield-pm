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
/// Unit tests for SendInvitationCommand and handler (TDD).
/// Tests written before implementation.
/// </summary>
public class SendInvitationTests
{
    private readonly Mock<IAppDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly List<Invitation> _invitations;
    private readonly Guid _testAccountId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public SendInvitationTests()
    {
        _invitations = new List<Invitation>();

        _dbContextMock = new Mock<IAppDbContext>();
        SetupInvitationsDbSet();
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                foreach (var invitation in _invitations.Where(i => i.Id == Guid.Empty))
                {
                    invitation.Id = Guid.NewGuid();
                }
            })
            .ReturnsAsync(1);

        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.AccountId).Returns(_testAccountId);
        _currentUserMock.Setup(x => x.UserId).Returns(_testUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.Role).Returns("Owner");

        _emailServiceMock = new Mock<IEmailService>();
    }

    private void SetupInvitationsDbSet()
    {
        var invitationsDbSetMock = _invitations.AsQueryable().BuildMockDbSet();
        invitationsDbSetMock.Setup(m => m.Add(It.IsAny<Invitation>())).Callback<Invitation>(_invitations.Add);
        _dbContextMock.Setup(x => x.Invitations).Returns(invitationsDbSetMock.Object);
    }

    #region Validator Tests

    [Fact]
    public void Validator_ValidEmail_PassesValidation()
    {
        // Arrange
        var validator = new SendInvitationCommandValidator();
        var command = new SendInvitationCommand("test@example.com");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validator_EmptyEmail_FailsValidation(string? email)
    {
        // Arrange
        var validator = new SendInvitationCommandValidator();
        var command = new SendInvitationCommand(email!);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validator_InvalidEmailFormat_FailsValidation(string email)
    {
        // Arrange
        var validator = new SendInvitationCommandValidator();
        var command = new SendInvitationCommand(email);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("email"));
    }

    #endregion

    #region Handler Tests

    [Fact]
    public async Task Handle_ValidInvitation_CreatesInvitationRecord()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _invitations.Should().ContainSingle();
        _invitations[0].Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Handle_ValidInvitation_SetsAccountIdFromCurrentUser()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].AccountId.Should().Be(_testAccountId);
    }

    [Fact]
    public async Task Handle_ValidInvitation_SetsInvitedByUserId()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].InvitedByUserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task Handle_ValidInvitation_SetsExpiryTo24Hours()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");
        var beforeUtc = DateTime.UtcNow.AddHours(23).AddMinutes(59);
        var afterUtc = DateTime.UtcNow.AddHours(24).AddMinutes(1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].ExpiresAt.Should().BeAfter(beforeUtc);
        _invitations[0].ExpiresAt.Should().BeBefore(afterUtc);
    }

    [Fact]
    public async Task Handle_ValidInvitation_HashesTokenBeforeStorage()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].TokenHash.Should().NotBeNullOrEmpty();
        // Token should be hashed, not raw - SHA256 produces 64 hex chars
        _invitations[0].TokenHash.Length.Should().Be(64);
    }

    [Fact]
    public async Task Handle_ValidInvitation_SendsEmail()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendInvitationEmailAsync(
                "newuser@example.com",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidInvitation_SendsRawTokenInEmail()
    {
        // Arrange
        string? capturedToken = null;
        _emailServiceMock
            .Setup(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((email, token, ct) => capturedToken = token);

        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedToken.Should().NotBeNullOrEmpty();
        // Raw token should be different from hash
        capturedToken.Should().NotBe(_invitations[0].TokenHash);
    }

    [Fact]
    public async Task Handle_DuplicatePendingInvitation_ReturnsError()
    {
        // Arrange
        _invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            Email = "existing@example.com",
            TokenHash = "somehash",
            InvitedByUserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(12), // Still valid
            AcceptedAt = null
        });

        // Update the mock to use current invitations
        SetupInvitationsDbSet();

        var handler = CreateHandler();
        var command = new SendInvitationCommand("existing@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("pending invitation");
    }

    [Fact]
    public async Task Handle_ExpiredInvitation_AllowsNewInvitation()
    {
        // Arrange - existing but expired invitation
        _invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            Email = "expired@example.com",
            TokenHash = "somehash",
            InvitedByUserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            AcceptedAt = null
        });

        SetupInvitationsDbSet();

        var handler = CreateHandler();
        var command = new SendInvitationCommand("expired@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AcceptedInvitation_AllowsNewInvitation()
    {
        // Arrange - existing but already accepted invitation
        _invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            AccountId = _testAccountId,
            Email = "accepted@example.com",
            TokenHash = "somehash",
            InvitedByUserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            AcceptedAt = DateTime.UtcNow.AddHours(-1) // Already accepted
        });

        SetupInvitationsDbSet();

        var handler = CreateHandler();
        var command = new SendInvitationCommand("accepted@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonOwnerRole_ThrowsUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.Role).Returns("Contributor");

        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ValidInvitation_CallsSaveChanges()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("newuser@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidInvitation_TrimsEmail()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("  newuser@example.com  ");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Handle_ValidInvitation_NormalizesEmailToLowercase()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new SendInvitationCommand("NewUser@Example.COM");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _invitations.Should().ContainSingle();
        _invitations[0].Email.Should().Be("newuser@example.com");
    }

    #endregion

    private SendInvitationCommandHandler CreateHandler()
    {
        return new SendInvitationCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _emailServiceMock.Object);
    }
}
