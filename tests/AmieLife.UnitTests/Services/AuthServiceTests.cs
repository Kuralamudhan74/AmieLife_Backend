using Xunit;
using AmieLife.Application.Common.Interfaces;
using AmieLife.Application.DTOs.Auth;
using AmieLife.Application.Services;
using AmieLife.Domain.Entities;
using AmieLife.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AmieLife.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IEmailVerificationTokenRepository> _emailVerificationRepo = new();
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();

    private AuthService CreateService() => new(
        _userRepo.Object,
        _refreshTokenRepo.Object,
        _emailVerificationRepo.Object,
        _passwordResetRepo.Object,
        _tokenService.Object,
        _emailService.Object,
        _logger.Object);

    // ── Login Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsGenericError()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(new LoginRequestDto("x@x.com", "pass"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_WithLockedAccount_ReturnsLockoutMessage()
    {
        var lockedUser = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsEmailVerified = true,
            Status = UserStatus.Active,
            LockoutEndTime = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com", default)).ReturnsAsync(lockedUser);

        var result = await CreateService().LoginAsync(new LoginRequestDto("test@test.com", "Test@1234"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Login_WithWrongPassword_RecordsFailedAttempt()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1"),
            IsEmailVerified = true,
            Status = UserStatus.Active,
            FailedLoginAttempts = 0
        };
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com", default)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);

        await CreateService().LoginAsync(new LoginRequestDto("test@test.com", "WrongPass@1"), default);

        _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.FailedLoginAttempts == 1), default), Times.Once);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ReturnsVerificationRequired()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "unverified@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsEmailVerified = false,
            Status = UserStatus.Active
        };
        _userRepo.Setup(r => r.GetByEmailAsync("unverified@test.com", default)).ReturnsAsync(user);

        var result = await CreateService().LoginAsync(new LoginRequestDto("unverified@test.com", "Test@1234"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("verify your email");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenPair()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "valid@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Valid@1234"),
            IsEmailVerified = true,
            Status = UserStatus.Active,
            Role = UserRole.Customer
        };
        _userRepo.Setup(r => r.GetByEmailAsync("valid@test.com", default)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access.token.here");
        _tokenService.Setup(t => t.GenerateAndStoreRefreshTokenAsync(It.IsAny<Guid>(), default)).ReturnsAsync("refresh.token.here");

        var result = await CreateService().LoginAsync(new LoginRequestDto("valid@test.com", "Valid@1234"), default);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access.token.here");
        result.Data.RefreshToken.Should().Be("refresh.token.here");
    }

    // ── Logout Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithValidToken_RevokesToken()
    {
        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "somehash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        _refreshTokenRepo
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(refreshToken);
        _refreshTokenRepo
            .Setup(r => r.RevokeAsync(It.IsAny<RefreshToken>(), default))
            .Returns(Task.CompletedTask);

        var result = await CreateService().LogoutAsync(new LogoutRequestDto("raw_token"), default);

        result.IsSuccess.Should().BeTrue();
        _refreshTokenRepo.Verify(r => r.RevokeAsync(refreshToken, default), Times.Once);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_StillReturnsSuccess()
    {
        _refreshTokenRepo.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().LogoutAsync(new LogoutRequestDto("invalid_token"), default);

        result.IsSuccess.Should().BeTrue(); // No token probing
    }

    // ── Signup Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Signup_WithExistingEmail_ReturnsFailure()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("existing@test.com", default)).ReturnsAsync(true);

        var result = await CreateService().SignupAsync(
            new SignupRequestDto("existing@test.com", "Pass@1234", "Pass@1234", null, null, null), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    // ── Account Lockout Tests ────────────────────────────────────────────────

    [Fact]
    public async Task Login_After5FailedAttempts_LocksAccount()
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = "lockme@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Right@1234"),
            IsEmailVerified = true,
            Status = UserStatus.Active,
            FailedLoginAttempts = 4 // One more will lock
        };
        _userRepo.Setup(r => r.GetByEmailAsync("lockme@test.com", default)).ReturnsAsync(user);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>(), default)).Returns(Task.CompletedTask);

        await CreateService().LoginAsync(new LoginRequestDto("lockme@test.com", "Wrong@1234"), default);

        _userRepo.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.LockoutEndTime.HasValue), default), Times.Once);
    }
}
