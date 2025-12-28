using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Application.Common.Interfaces;
using PropertyManager.Domain.Entities;
using PropertyManager.Infrastructure.Identity;
using PropertyManager.Infrastructure.Persistence;

namespace PropertyManager.Api.Tests;

public class AuthControllerTests : IClassFixture<PropertyManagerWebApplicationFactory>
{
    private readonly PropertyManagerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(PropertyManagerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ==================== INVITATION TESTS ====================

    [Fact]
    public async Task SendInvitation_WithValidEmail_Returns200()
    {
        // Arrange - Need an authenticated Owner user to send invitations
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var accessToken = await LoginAndGetToken(ownerEmail, ownerPassword);

        var inviteeEmail = $"invitee{Guid.NewGuid():N}@example.com";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/invite");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(new { Email = inviteeEmail });

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<SendInvitationResponse>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();

        // Verify invitation email was sent
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentInvitationEmails.FirstOrDefault(e => e.Email == inviteeEmail);
        sentEmail.Should().NotBeNull();
    }

    [Fact]
    public async Task SendInvitation_WithoutAuth_Returns401()
    {
        // Arrange
        var inviteeEmail = $"invitee{Guid.NewGuid():N}@example.com";

        // Act - No Authorization header
        var response = await _client.PostAsJsonAsync("/api/v1/auth/invite", new { Email = inviteeEmail });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendInvitation_WithInvalidEmail_Returns400()
    {
        // Arrange
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var accessToken = await LoginAndGetToken(ownerEmail, ownerPassword);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/invite");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(new { Email = "not-an-email" });

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== REGISTRATION WITH INVITATION TESTS ====================

    [Fact]
    public async Task Register_WithValidInvitationToken_Returns201()
    {
        // Arrange - Create owner, send invitation, get token
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var inviteeEmail = $"newuser{Guid.NewGuid():N}@example.com";

        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, inviteeEmail);

        var registerRequest = new { Password = "Test@123456", Token = invitationToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content.Should().NotBeNull();
        content!.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithInvitation_UserIsAutoVerified()
    {
        // Arrange
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var inviteeEmail = $"autoverify{Guid.NewGuid():N}@example.com";

        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, inviteeEmail);

        // Register
        var registerRequest = new { Password = "Test@123456", Token = invitationToken };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to login immediately (should work since email is auto-verified)
        var loginRequest = new { Email = inviteeEmail, Password = "Test@123456" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert - Login should succeed without email verification
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithoutToken_Returns400()
    {
        // Arrange - Missing token
        var registerRequest = new { Password = "Test@123456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidToken_Returns400()
    {
        // Arrange
        var registerRequest = new { Password = "Test@123456", Token = "invalid-token-12345" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("invalid or expired");
    }

    [Fact]
    public async Task Register_WithExpiredToken_Returns400()
    {
        // Arrange - Create an expired invitation directly in DB
        var (_, _, accountId) = await CreateOwnerUser();
        var expiredEmail = $"expired{Guid.NewGuid():N}@example.com";
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = ComputeTokenHash(token);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get any user ID for InvitedByUserId
            var invitingUser = await dbContext.Users.FirstAsync();

            var invitation = new Invitation
            {
                AccountId = accountId,
                Email = expiredEmail,
                TokenHash = tokenHash,
                InvitedByUserId = invitingUser.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
                CreatedAt = DateTime.UtcNow.AddHours(-25)
            };
            dbContext.Invitations.Add(invitation);
            await dbContext.SaveChangesAsync();
        }

        var registerRequest = new { Password = "Test@123456", Token = token };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.ToLower().Should().Contain("invalid or expired");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400WithValidationErrors()
    {
        // Arrange
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var inviteeEmail = $"weakpwd{Guid.NewGuid():N}@example.com";

        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, inviteeEmail);

        var registerRequest = new { Password = "weak", Token = invitationToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Password");
    }

    [Fact]
    public async Task Register_TokenCanOnlyBeUsedOnce()
    {
        // Arrange
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();
        var inviteeEmail = $"onceonly{Guid.NewGuid():N}@example.com";

        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, inviteeEmail);

        // First registration should succeed
        var firstRegisterRequest = new { Password = "Test@123456", Token = invitationToken };
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", firstRegisterRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to use the same token again
        var secondRegisterRequest = new { Password = "Test@789012", Token = invitationToken };
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", secondRegisterRequest);

        // Assert - Token should be invalid after first use
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== LOGIN TESTS (AC4.1, AC4.3, AC4.4) ====================

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithJwtToken()
    {
        // Arrange - Create a verified user via invitation
        var email = $"login{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert (AC4.1)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.ExpiresIn.Should().Be(3600); // 60 minutes in seconds

        // Verify refresh token cookie was set
        response.Headers.Should().ContainKey("Set-Cookie");
        var setCookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        setCookieHeader.Should().Contain("refreshToken=");
        setCookieHeader!.ToLower().Should().Contain("httponly");
        setCookieHeader.ToLower().Should().Contain("secure");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401WithGenericMessage()
    {
        // Arrange
        var email = $"loginwrongpwd{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = "WrongPassword@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert (AC4.3 - generic error message)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
        // Should NOT reveal whether email exists
        content.ToLower().Should().NotContain("user not found");
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_Returns401WithGenericMessage()
    {
        // Arrange
        var loginRequest = new
        {
            Email = $"nonexistent{Guid.NewGuid():N}@example.com",
            Password = "Test@123456"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert (AC4.3 - generic error message, no user enumeration)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithMissingEmail_Returns400()
    {
        // Arrange
        var loginRequest = new { Password = "Test@123456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithMissingPassword_Returns400()
    {
        // Arrange
        var loginRequest = new { Email = "test@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== JWT TOKEN TESTS (AC4.2) ====================

    [Fact]
    public async Task Login_JwtContainsRequiredClaims()
    {
        // Arrange
        var email = $"jwtclaims{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert - decode JWT and verify claims (AC4.2)
        var jwt = content!.AccessToken;
        var payload = DecodeJwtPayload(jwt);

        payload.Should().ContainKey("userId");
        payload.Should().ContainKey("accountId");
        payload.Should().ContainKey("role");
        payload.Should().ContainKey("exp");

        // Verify values are valid GUIDs
        Guid.TryParse(payload["userId"].ToString(), out var userId).Should().BeTrue();
        Guid.TryParse(payload["accountId"].ToString(), out var accountId).Should().BeTrue();
        payload["role"].ToString().Should().Be("Owner");

        // Verify expiration is ~60 minutes from now
        var exp = long.Parse(payload["exp"].ToString()!);
        var expDateTime = DateTimeOffset.FromUnixTimeSeconds(exp);
        var expectedExp = DateTimeOffset.UtcNow.AddMinutes(60);
        expDateTime.Should().BeCloseTo(expectedExp, TimeSpan.FromMinutes(1));
    }

    // ==================== REFRESH TOKEN TESTS (AC4.6) ====================

    [Fact]
    public async Task Refresh_WithValidRefreshToken_Returns200WithNewAccessToken()
    {
        // Arrange - Login first
        var email = $"refresh{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");

        // Create request with refresh token cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        foreach (var cookie in cookies)
        {
            refreshRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act
        var refreshResponse = await _client.SendAsync(refreshRequest);

        // Assert (AC4.6)
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task Refresh_WithoutRefreshToken_Returns401()
    {
        // Act - Call refresh without any cookie
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No refresh token");
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_Returns401()
    {
        // Arrange
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", "refreshToken=invalidtoken");

        // Act
        var response = await _client.SendAsync(refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==================== CONCURRENT SESSIONS TEST (AC4.7) ====================

    [Fact]
    public async Task Login_MultipleTimes_CreatesSeparateSessions()
    {
        // Arrange - Create a verified user
        var email = $"concurrent{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };

        // Act - Login twice (simulating different devices/browsers)
        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert (AC4.7 - multiple concurrent sessions allowed)
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var token1 = (await response1.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        var token2 = (await response2.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        // Tokens should be different (different JTI)
        token1.Should().NotBe(token2);

        // Both tokens should be valid for the same user
        var payload1 = DecodeJwtPayload(token1);
        var payload2 = DecodeJwtPayload(token2);

        payload1["userId"].ToString().Should().Be(payload2["userId"].ToString());
        payload1["jti"].ToString().Should().NotBe(payload2["jti"].ToString());
    }

    // ==================== LOGOUT TESTS (AC5.1, AC5.2, AC5.3) ====================

    [Fact]
    public async Task Logout_WithValidSession_Returns204()
    {
        // Arrange - Login first
        var email = $"logout{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Extract access token and cookies from login response
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginContent!.AccessToken;

        // Verify token is valid JWT format
        accessToken.Should().NotBeNullOrEmpty("Access token should be returned from login");
        accessToken.Split('.').Length.Should().Be(3, "Access token should be valid JWT with 3 parts");

        // Act - Create request with Authorization header on the request itself
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Authorization", $"Bearer {accessToken}");

        var logoutResponse = await _client.SendAsync(logoutRequest);

        // Assert (AC5.1)
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_RefreshTokenDeletedFromDatabase()
    {
        // Arrange - Login first
        var email = $"logoutdb{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var accessToken = loginContent!.AccessToken;
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");

        // Extract user ID to verify token deletion
        var payload = DecodeJwtPayload(accessToken);
        var userId = Guid.Parse(payload["userId"]!.ToString()!);

        // Verify refresh token exists before logout
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tokensBeforeLogout = await dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .CountAsync();
            tokensBeforeLogout.Should().BeGreaterThan(0);
        }

        // Logout - send with cookies (refresh token invalidation uses cookie, not JWT)
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        foreach (var cookie in cookies)
        {
            logoutRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var logoutResponse = await _client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert (AC5.2) - Token should be revoked in database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var activeTokensAfterLogout = await dbContext.RefreshTokens
                .IgnoreQueryFilters()
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .CountAsync();
            activeTokensAfterLogout.Should().Be(0);
        }
    }

    [Fact]
    public async Task Logout_InvalidatedRefreshTokenReturns401OnRefresh()
    {
        // Arrange - Login first
        var email = $"logoutrefresh{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();

        // Logout - send with cookies
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        foreach (var cookie in cookies)
        {
            logoutRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        await _client.SendAsync(logoutRequest);

        // Act - Try to refresh with the invalidated token (AC5.3)
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        foreach (var cookie in cookies)
        {
            refreshRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var refreshResponse = await _client.SendAsync(refreshRequest);

        // Assert - Should return 401 because token was invalidated
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutRefreshToken_Returns204_IdempotentBehavior()
    {
        // Logout is idempotent - calling it without a session should still succeed
        // The key security is that the cookie is cleared and no sensitive operation occurs
        // This matches AC5.2: "Logout should be idempotent - calling it multiple times is safe"

        // Act - Call logout without any authentication or cookies
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert - Returns 204 (logout is idempotent, no error for missing token)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_MultipleDevices_OnlyCurrentDeviceAffected()
    {
        // Arrange - Create a verified user
        var email = $"logoutmulti{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var loginRequest = new { Email = email, Password = password };

        // Login from "device 1"
        var device1LoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        device1LoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var device1Cookies = device1LoginResponse.Headers.GetValues("Set-Cookie").ToList();

        // Login from "device 2"
        var device2LoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        device2LoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var device2Cookies = device2LoginResponse.Headers.GetValues("Set-Cookie").ToList();

        // Logout from device 1
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        foreach (var cookie in device1Cookies)
        {
            logoutRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }
        await _client.SendAsync(logoutRequest);

        // Act - Try to refresh from device 2 (should still work) (AC5.2)
        var device2RefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        foreach (var cookie in device2Cookies)
        {
            device2RefreshRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var device2RefreshResponse = await _client.SendAsync(device2RefreshRequest);

        // Assert - Device 2 should still be able to refresh
        device2RefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==================== PASSWORD RESET TESTS (AC6.1, AC6.3, AC6.4, AC6.5) ====================

    [Fact]
    public async Task ForgotPassword_WithValidEmail_Returns204()
    {
        // Arrange - Create a verified user
        var email = $"forgot{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        await CreateVerifiedUser(email, password);

        var request = new { Email = email };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert (AC6.1 - always returns 204)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify password reset email was sent
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentPasswordResetEmails.FirstOrDefault(e => e.Email == email);
        sentEmail.Should().NotBeNull("Password reset email should have been sent");
    }

    [Fact]
    public async Task ForgotPassword_WithNonexistentEmail_Returns204_NoEmailSent()
    {
        // Arrange
        var nonexistentEmail = $"nonexistent{Guid.NewGuid():N}@example.com";
        var request = new { Email = nonexistentEmail };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert (AC6.1 - returns 204 even for non-existent email to prevent enumeration)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify NO email was sent (user doesn't exist)
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var emailExists = fakeEmailService.SentPasswordResetEmails.Any(e => e.Email == nonexistentEmail);
        emailExists.Should().BeFalse("No email should be sent for non-existent user");
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_Returns400()
    {
        // Arrange
        var request = new { Email = "not-an-email" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_Returns204()
    {
        // Arrange - Create verified user, then request password reset
        var email = $"reset{Guid.NewGuid():N}@example.com";
        var oldPassword = "Test@123456";
        var newPassword = "NewPass@789012";

        await CreateVerifiedUser(email, oldPassword);

        // Request password reset
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });

        // Get reset token
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentPasswordResetEmails.FirstOrDefault(e => e.Email == email);
        sentEmail.Should().NotBeNull();
        var token = sentEmail!.Token;

        var resetRequest = new { Token = token, NewPassword = newPassword };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        // Assert (AC6.3)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify can login with new password
        var loginRequest = new { Email = email, Password = newPassword };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        // Arrange
        var invalidToken = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{Guid.NewGuid()}:invalidtoken"));

        var request = new { Token = invalidToken, NewPassword = "NewPass@789012" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert (AC6.5 - generic error message)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("invalid or expired");
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_Returns400()
    {
        // Arrange - Create verified user, then request password reset
        var email = $"resetweak{Guid.NewGuid():N}@example.com";
        var oldPassword = "Test@123456";

        await CreateVerifiedUser(email, oldPassword);

        // Request password reset
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });

        // Get reset token
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentPasswordResetEmails.FirstOrDefault(e => e.Email == email);
        var token = sentEmail!.Token;

        var resetRequest = new { Token = token, NewPassword = "weak" }; // Too short, no requirements

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Password");
    }

    [Fact]
    public async Task ResetPassword_InvalidatesAllSessions()
    {
        // Arrange - Create verified user, login, then reset password
        var email = $"resetsession{Guid.NewGuid():N}@example.com";
        var oldPassword = "Test@123456";
        var newPassword = "NewPass@789012";

        await CreateVerifiedUser(email, oldPassword);

        // Login to create a session
        var loginRequest = new { Email = email, Password = oldPassword };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();

        // Request password reset
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });

        // Get reset token
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentPasswordResetEmails.FirstOrDefault(e => e.Email == email);
        var token = sentEmail!.Token;

        // Reset password
        var resetRequest = new { Token = token, NewPassword = newPassword };
        var resetResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);
        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Try to use the old refresh token (AC6.4)
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        foreach (var cookie in cookies)
        {
            refreshRequest.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var refreshResponse = await _client.SendAsync(refreshRequest);

        // Assert - Old session should be invalidated
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_TokenCanOnlyBeUsedOnce()
    {
        // Arrange
        var email = $"resetonce{Guid.NewGuid():N}@example.com";
        var oldPassword = "Test@123456";
        var newPassword1 = "NewPass@789012";
        var newPassword2 = "AnotherPass@345678";

        await CreateVerifiedUser(email, oldPassword);

        // Request password reset
        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });

        // Get reset token
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentPasswordResetEmails.FirstOrDefault(e => e.Email == email);
        var token = sentEmail!.Token;

        // First reset should succeed
        var firstResetRequest = new { Token = token, NewPassword = newPassword1 };
        var firstResetResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", firstResetRequest);
        firstResetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Try to use the same token again
        var secondResetRequest = new { Token = token, NewPassword = newPassword2 };
        var secondResetResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", secondResetRequest);

        // Assert - Token should be invalid after first use
        secondResetResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await secondResetResponse.Content.ReadAsStringAsync();
        content.Should().Contain("invalid or expired");
    }

    // ==================== HELPER METHODS ====================

    /// <summary>
    /// Creates an owner user directly in the database (bypasses invitation).
    /// Used to bootstrap tests that need an owner to send invitations.
    /// </summary>
    private async Task<(string email, string password, Guid accountId)> CreateOwnerUser()
    {
        var email = $"owner{Guid.NewGuid():N}@example.com";
        var password = "Test@123456";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create account
        var account = new Account { Name = email.Split('@')[0] };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        // Create user with email confirmed
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            AccountId = account.Id,
            Role = "Owner"
        };

        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return (email, password, account.Id);
    }

    /// <summary>
    /// Creates a verified user via the invitation flow.
    /// </summary>
    private async Task CreateVerifiedUser(string email, string password)
    {
        // Create an owner to send the invitation
        var (ownerEmail, ownerPassword, _) = await CreateOwnerUser();

        // Send invitation and get token
        var invitationToken = await SendInvitationAndGetToken(ownerEmail, ownerPassword, email);

        // Register with the invitation token
        var registerRequest = new { Password = password, Token = invitationToken };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>
    /// Sends an invitation and returns the token from the fake email service.
    /// </summary>
    private async Task<string> SendInvitationAndGetToken(string ownerEmail, string ownerPassword, string inviteeEmail)
    {
        var accessToken = await LoginAndGetToken(ownerEmail, ownerPassword);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/invite");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(new { Email = inviteeEmail });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get the invitation token from the fake email service
        var fakeEmailService = _factory.Services.GetRequiredService<FakeEmailService>();
        var sentEmail = fakeEmailService.SentInvitationEmails.FirstOrDefault(e => e.Email == inviteeEmail);
        sentEmail.Should().NotBeNull($"Invitation email should have been sent to {inviteeEmail}");

        return sentEmail!.Token;
    }

    /// <summary>
    /// Logs in and returns the access token.
    /// </summary>
    private async Task<string> LoginAndGetToken(string email, string password)
    {
        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return content!.AccessToken;
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private Dictionary<string, object?> DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        parts.Length.Should().Be(3);

        var payload = parts[1];
        // Add padding if needed
        var paddedPayload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(paddedPayload);
        var json = Encoding.UTF8.GetString(bytes);

        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
    }

    private record RegisterResponse(string UserId);
    private record LoginResponse(string AccessToken, int ExpiresIn);
    private record RefreshResponse(string AccessToken, int ExpiresIn);
    private record SendInvitationResponse(bool Success, string Message);
}
