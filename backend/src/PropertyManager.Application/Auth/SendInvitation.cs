using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;

namespace PropertyManager.Application.Auth;

/// <summary>
/// Command for sending an invitation email.
/// </summary>
public record SendInvitationCommand(string Email) : IRequest<SendInvitationResult>;

/// <summary>
/// Result of sending an invitation.
/// </summary>
public record SendInvitationResult(bool Success, string? Error = null);

/// <summary>
/// Validator for SendInvitationCommand.
/// </summary>
public class SendInvitationCommandValidator : AbstractValidator<SendInvitationCommand>
{
    public SendInvitationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

/// <summary>
/// Handler for SendInvitationCommand.
/// Creates invitation record with hashed token and sends email.
/// </summary>
public class SendInvitationCommandHandler : IRequestHandler<SendInvitationCommand, SendInvitationResult>
{
    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IEmailService _emailService;

    public SendInvitationCommandHandler(
        IAppDbContext dbContext,
        ICurrentUser currentUser,
        IEmailService emailService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _emailService = emailService;
    }

    public async Task<SendInvitationResult> Handle(SendInvitationCommand request, CancellationToken cancellationToken)
    {
        // Only Owner role can send invitations
        if (_currentUser.Role != "Owner")
        {
            throw new UnauthorizedAccessException("Only account owners can send invitations.");
        }

        // Normalize email
        var email = request.Email.Trim().ToLowerInvariant();

        // Check for existing pending invitation
        var existingPendingInvitation = await _dbContext.Invitations
            .Where(i => i.Email == email && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPendingInvitation != null)
        {
            return new SendInvitationResult(false, "A pending invitation already exists for this email address.");
        }

        // Generate token (raw and hashed)
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        // Create invitation record
        var invitation = new Invitation
        {
            AccountId = _currentUser.AccountId,
            Email = email,
            TokenHash = tokenHash,
            InvitedByUserId = _currentUser.UserId,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _dbContext.Invitations.Add(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send invitation email with raw token
        await _emailService.SendInvitationEmailAsync(email, rawToken, cancellationToken);

        return new SendInvitationResult(true);
    }

    private static string GenerateSecureToken()
    {
        // Generate 32 random bytes and encode as URL-safe base64
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
