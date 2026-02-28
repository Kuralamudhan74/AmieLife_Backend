using Xunit;
using System.Net;
using System.Net.Http.Json;
using AmieLife.Application.DTOs.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AmieLife.IntegrationTests.Auth;

/// <summary>
/// Integration tests that spin up the real ASP.NET Core pipeline via WebApplicationFactory.
/// These tests require a real (or in-memory) database connection.
/// For CI, configure a test database via environment variables.
///
/// To run locally:
/// 1. Set the ASPNETCORE_ENVIRONMENT to "Testing"
/// 2. Configure a test database in appsettings.Testing.json (gitignored)
/// </summary>
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Signup_WithValidData_Returns200()
    {
        var request = new SignupRequestDto(
            Email: $"test_{Guid.NewGuid():N}@amielife.test",
            Password: "TestPass@123",
            ConfirmPassword: "TestPass@123",
            FirstName: "Test",
            LastName: "User",
            PhoneNumber: null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signup_WithWeakPassword_Returns400()
    {
        var request = new SignupRequestDto(
            Email: "weak@amielife.test",
            Password: "weak",
            ConfirmPassword: "weak",
            FirstName: null, LastName: null, PhoneNumber: null);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithNonExistentAccount_Returns400WithGenericMessage()
    {
        var request = new LoginRequestDto("nobody@amielife.test", "Whatever@123");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("does not exist"); // No email enumeration
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var request = new RefreshTokenRequestDto("this-is-not-a-valid-token");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_StillReturns200()
    {
        var request = new LogoutRequestDto("invalid-refresh-token");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithAnyEmail_AlwaysReturns200()
    {
        var request = new ForgotPasswordRequestDto("nonexistent@test.com");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
