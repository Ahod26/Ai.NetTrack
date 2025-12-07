using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using backend.Services.Classes.Auth;
using backend.Models.Configuration;
using backend.Models.Domain;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.UnitTests.Services.Auth;

public class TokenServiceTests
{
  private readonly JwtSettings _jwtSettings;
  private readonly TokenService _tokenService;

  public TokenServiceTests()
  {
    _jwtSettings = new JwtSettings
    {
      SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHS256Algorithm",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpirationInMinutes = 60
    };

    var options = Options.Create(_jwtSettings);
    _tokenService = new TokenService(options);
  }

  /// <summary>
  /// Tests that GenerateToken creates a valid JWT token string for a user with roles.
  /// Verifies the token is not null or empty and can be read by the JWT handler.
  /// </summary>
  [Fact]
  public void GenerateToken_WithValidUser_ReturnsValidJwtToken()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "premium", "admin" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    token.Should().NotBeNullOrEmpty();
    var handler = new JwtSecurityTokenHandler();
    handler.CanReadToken(token).Should().BeTrue();
  }

  /// <summary>
  /// Tests that the generated JWT token contains all required user claims:
  /// NameIdentifier (user ID), Email, Name (full name), and IsNewsletterSubscribed.
  /// </summary>
  [Fact]
  public void GenerateToken_TokenContainsAllUserClaims()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "premium" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.FullName);
    jwtToken.Claims.Should().Contain(c => c.Type == "IsNewsletterSubscribed" && c.Value == "True");
    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "premium");
  }

  /// <summary>
  /// Tests that when a user has multiple roles, all roles are included as separate Role claims in the JWT.
  /// </summary>
  [Fact]
  public void GenerateToken_WithMultipleRoles_ContainsAllRoleClaims()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "admin@example.com",
      FullName = "Admin User",
      IsSubscribedToNewsletter = false
    };

    var roles = new List<string> { "premium", "admin", "moderator" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    roleClaims.Should().BeEquivalentTo(roles);
  }

  /// <summary>
  /// Tests that when a user is not subscribed to the newsletter, the IsNewsletterSubscribed claim is set to "False".
  /// </summary>
  [Fact]
  public void GenerateToken_NewsletterFalse_ContainsFalseInClaim()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = false
    };

    var roles = new List<string> { "premium" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Claims.Should().Contain(c => c.Type == "IsNewsletterSubscribed" && c.Value == "False");
  }

  /// <summary>
  /// Tests that the generated JWT token contains the correct Issuer and Audience from JWT configuration settings.
  /// </summary>
  [Fact]
  public void GenerateToken_TokenContainsIssuerAndAudience()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "premium" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
    jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
  }

  /// <summary>
  /// Tests that the JWT token expiration time is set correctly based on the configured ExpirationInMinutes.
  /// The test adds a 1-second tolerance margin because JWT ValidTo rounds to seconds precision.
  /// </summary>
  [Fact]
  public void GenerateToken_TokenExpirationIsSetCorrectly()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "premium" };
    var beforeGeneration = DateTime.UtcNow;

    // Act
    var token = _tokenService.GenerateToken(user, roles);
    var afterGeneration = DateTime.UtcNow;

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    // Add 1-second tolerance because JWT ValidTo is rounded to seconds
    var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.ExpirationInMinutes).AddSeconds(-1);
    var expectedExpirationMax = afterGeneration.AddMinutes(_jwtSettings.ExpirationInMinutes).AddSeconds(1);

    jwtToken.ValidTo.Should().BeOnOrAfter(expectedExpiration);
    jwtToken.ValidTo.Should().BeOnOrBefore(expectedExpirationMax);
  }

  /// <summary>
  /// Tests that GenerateToken works correctly with an empty roles list, producing a valid token without any Role claims.
  /// The token should still contain other standard claims like Email and Name.
  /// </summary>
  [Fact]
  public void GenerateToken_WithEmptyRoles_GeneratesTokenWithoutRoleClaims()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string>();

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    // But should still contain other claims
    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email);
  }

  /// <summary>
  /// Tests that the JWT token is signed using the HS256 (HMAC-SHA256) algorithm.
  /// </summary>
  [Fact]
  public void GenerateToken_UsesHS256Algorithm()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user123",
      Email = "test@example.com",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "premium" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Header.Alg.Should().Be("HS256");
  }

  /// <summary>
  /// Tests that when a user is subscribed to the newsletter, the JWT token contains the IsNewsletterSubscribed claim set to "True".
  /// </summary>
  [Fact]
  public void GenerateToken_WithNewsletterSubscription_ContainsNewsletterClaim()
  {
    // Arrange
    var user = new ApiUser
    {
      Id = "user456",
      Email = "subscriber@example.com",
      FullName = "Newsletter Subscriber",
      IsSubscribedToNewsletter = true
    };

    var roles = new List<string> { "free" };

    // Act
    var token = _tokenService.GenerateToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Claims.Should().Contain(c => c.Type == "IsNewsletterSubscribed" && c.Value == "True");
  }
}
