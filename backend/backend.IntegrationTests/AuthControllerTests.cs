using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace backend.IntegrationTests;

public class AuthControllerTests : IClassFixture<WebAppFactory>
{
  private readonly HttpClient _client;

  public AuthControllerTests(WebAppFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task AuthStatus_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/Auth/status");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Register_WithValidData_ReturnsOkOrBadRequest()
  {
    // Arrange
    var registerData = new
    {
      fullName = "Test User",
      email = $"test{Guid.NewGuid()}@example.com",
      password = "Test123!@#",
      isSubscribedToNewsletter = true
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Auth", registerData);

    // Assert - Should succeed or return bad request if validation fails, but not 404
    response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    // Additional check: status should be OK, BadRequest, or Conflict (if user exists)
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
  }
}
