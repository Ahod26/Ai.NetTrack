using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using backend.Services.Classes.Auth;
using backend.Services.Interfaces.Auth;
using backend.Services.Interfaces;
using backend.Repository.Interfaces;
using backend.Models.Dtos;
using backend.Models.Domain;

namespace backend.UnitTests.Services.Auth;

public class AuthServiceTests
{
  private readonly Mock<ICookieService> _mockCookieService;
  private readonly Mock<ITokenService> _mockTokenService;
  private readonly Mock<IAuthRepo> _mockAuthRepo;
  private readonly Mock<IMapper> _mockMapper;
  private readonly Mock<IEmailListCacheService> _mockEmailListCacheService;
  private readonly Mock<ILogger<AuthService>> _mockLogger;
  private readonly AuthService _authService;

  public AuthServiceTests()
  {
    _mockCookieService = new Mock<ICookieService>();
    _mockTokenService = new Mock<ITokenService>();
    _mockAuthRepo = new Mock<IAuthRepo>();
    _mockMapper = new Mock<IMapper>();
    _mockEmailListCacheService = new Mock<IEmailListCacheService>();
    _mockLogger = new Mock<ILogger<AuthService>>();

    _authService = new AuthService(
        _mockCookieService.Object,
        _mockTokenService.Object,
        _mockAuthRepo.Object,
        _mockMapper.Object,
        _mockEmailListCacheService.Object,
        _mockLogger.Object
    );
  }

  #region RegisterAsync Tests

  /// <summary>
  /// Tests that RegisterAsync returns a failure response when attempting to register with an email that already exists in the system.
  /// The service should not attempt to create a new user when the email is already taken.
  /// </summary>
  [Fact]
  public async Task RegisterAsync_WithExistingEmail_ReturnsFailureWithEmailExistsError()
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "existing@example.com",
      Password = "Password123!",
      FullName = "Test User",
      IsSubscribedToNewsletter = true
    };

    var existingUser = new ApiUser { Email = registerDto.Email };
    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync(existingUser);

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Registration failed");
    result.Errors.Should().ContainSingle()
        .Which.Should().Be("Email address is already registered");
    _mockAuthRepo.Verify(x => x.CreateAsync(It.IsAny<ApiUser>(), It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Tests the complete successful registration flow: creating a new user, assigning the "premium" role, and adding to newsletter if subscribed.
  /// Verifies that all user properties are correctly set and dependencies are called appropriately.
  /// </summary>
  [Fact]
  public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsSuccess()
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "newuser@example.com",
      Password = "Password123!",
      FullName = "New User",
      IsSubscribedToNewsletter = true
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync((ApiUser?)null);

    var identityResult = IdentityResult.Success;
    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), registerDto.Password))
        .ReturnsAsync(identityResult);

    _mockAuthRepo.Setup(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"))
        .ReturnsAsync(IdentityResult.Success);

    _mockEmailListCacheService.Setup(x => x.ToggleUserFromNewsletterAsync(It.IsAny<EmailNewsletterDTO>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Registration successful");
    result.Errors.Should().BeEmpty();

    _mockAuthRepo.Verify(x => x.CreateAsync(
        It.Is<ApiUser>(u =>
            u.Email == registerDto.Email &&
            u.FullName == registerDto.FullName &&
            u.IsSubscribedToNewsletter == true),
        registerDto.Password), Times.Once);

    _mockAuthRepo.Verify(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"), Times.Once);
    _mockEmailListCacheService.Verify(x => x.ToggleUserFromNewsletterAsync(
        It.Is<EmailNewsletterDTO>(dto => dto.Email == registerDto.Email)), Times.Once);
  }

  /// <summary>
  /// Tests that when a user registers with IsSubscribedToNewsletter set to false, the newsletter service is not called.
  /// The registration should still succeed, but the user is not added to the newsletter list.
  /// </summary>
  [Fact]
  public async Task RegisterAsync_WithNewsletterFalse_DoesNotAddToNewsletter()
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "newuser@example.com",
      Password = "Password123!",
      FullName = "New User",
      IsSubscribedToNewsletter = false
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync((ApiUser?)null);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), registerDto.Password))
        .ReturnsAsync(IdentityResult.Success);

    _mockAuthRepo.Setup(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"))
        .ReturnsAsync(IdentityResult.Success);

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeTrue();
    _mockEmailListCacheService.Verify(x => x.ToggleUserFromNewsletterAsync(It.IsAny<EmailNewsletterDTO>()), Times.Never);
  }

  /// <summary>
  /// Tests that password validation errors from Identity (like PasswordTooShort, PasswordRequiresDigit) are correctly mapped to user-friendly error messages.
  /// Each Identity error code should produce a specific, understandable error message.
  /// </summary>
  [Theory]
  [InlineData("PasswordTooShort", "Password must be at least 6 characters long")]
  [InlineData("PasswordRequiresDigit", "Password must contain at least one number")]
  [InlineData("PasswordRequiresLower", "Password must contain at least one lowercase letter")]
  [InlineData("PasswordRequiresUpper", "Password must contain at least one uppercase letter")]
  [InlineData("PasswordRequiresNonAlphanumeric", "Password must contain at least one special character")]
  public async Task RegisterAsync_WithPasswordValidationError_ReturnsMappedErrorMessage(string errorCode, string expectedMessage)
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "newuser@example.com",
      Password = "weak",
      FullName = "New User",
      IsSubscribedToNewsletter = false
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync((ApiUser?)null);

    var identityError = new IdentityError { Code = errorCode, Description = "Default description" };
    var identityResult = IdentityResult.Failed(identityError);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), registerDto.Password))
        .ReturnsAsync(identityResult);

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Errors.Should().Contain(expectedMessage);
  }

  /// <summary>
  /// Tests that when registration fails with an unknown Identity error code, the original error description from Identity is preserved.
  /// </summary>
  [Fact]
  public async Task RegisterAsync_WithUnknownErrorCode_ReturnsOriginalDescription()
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "newuser@example.com",
      Password = "Password123!",
      FullName = "New User",
      IsSubscribedToNewsletter = false
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync((ApiUser?)null);

    var identityError = new IdentityError { Code = "UnknownError", Description = "Some unknown error occurred" };
    var identityResult = IdentityResult.Failed(identityError);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), registerDto.Password))
        .ReturnsAsync(identityResult);

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Errors.Should().Contain("Some unknown error occurred");
  }

  /// <summary>
  /// Tests that when user creation succeeds but role assignment fails, the registration returns a failure response.
  /// This ensures transactional integrity - partial success is treated as failure.
  /// </summary>
  [Fact]
  public async Task RegisterAsync_WhenRoleAssignmentFails_ReturnsFailure()
  {
    // Arrange
    var registerDto = new RegisterDTO
    {
      Email = "newuser@example.com",
      Password = "Password123!",
      FullName = "New User",
      IsSubscribedToNewsletter = false
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(registerDto.Email))
        .ReturnsAsync((ApiUser?)null);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), registerDto.Password))
        .ReturnsAsync(IdentityResult.Success);

    var roleError = new IdentityError { Code = "RoleError", Description = "Role assignment failed" };
    _mockAuthRepo.Setup(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"))
        .ReturnsAsync(IdentityResult.Failed(roleError));

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Errors.Should().Contain("Role assignment failed");
  }

  #endregion

  #region LoginAsync Tests

  /// <summary>
  /// Tests successful login flow: user exists with valid password, has roles, generates JWT token and sets authentication cookie.
  /// Verifies all authentication artifacts (token, cookie) are created correctly and UserInfo is populated.
  /// </summary>
  [Fact]
  public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithUserInfo()
  {
    // Arrange
    var loginDto = new LoginDTO
    {
      Email = "user@example.com",
      Password = "Password123!"
    };

    var user = new ApiUser
    {
      Id = "user123",
      Email = loginDto.Email,
      FullName = "Test User"
    };

    var roles = new List<string> { "premium" };
    var expectedToken = "generated.jwt.token";

    var apiUserDto = new ApiUserDto
    {
      Email = user.Email,
      FullName = user.FullName
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(loginDto.Email))
        .ReturnsAsync(user);

    _mockAuthRepo.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
        .ReturnsAsync(true);

    _mockAuthRepo.Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(roles);

    _mockTokenService.Setup(x => x.GenerateToken(user, roles))
        .Returns(expectedToken);

    _mockMapper.Setup(x => x.Map<ApiUserDto>(user))
        .Returns(apiUserDto);

    // Act
    var result = await _authService.LoginAsync(loginDto);

    // Assert
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Login successful");
    result.UserInfo.Should().NotBeNull();
    result.UserInfo!.ApiUserDto.Should().BeEquivalentTo(apiUserDto);
    result.UserInfo.Roles.Should().BeEquivalentTo(roles);

    _mockCookieService.Verify(x => x.SetAuthCookie(expectedToken), Times.Once);
  }

  /// <summary>
  /// Tests that when a user provides incorrect password, login fails with generic error message.
  /// No token or cookie should be generated for failed authentication attempts.
  /// </summary>
  [Fact]
  public async Task LoginAsync_WithInvalidPassword_ReturnsFailure()
  {
    // Arrange
    var loginDto = new LoginDTO
    {
      Email = "user@example.com",
      Password = "WrongPassword"
    };

    var user = new ApiUser { Id = "user123", Email = loginDto.Email };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(loginDto.Email))
        .ReturnsAsync(user);

    _mockAuthRepo.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
        .ReturnsAsync(false);

    // Act
    var result = await _authService.LoginAsync(loginDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Invalid email or password");
    result.UserInfo.Should().BeNull();

    _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApiUser>(), It.IsAny<List<string>>()), Times.Never);
    _mockCookieService.Verify(x => x.SetAuthCookie(It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Tests that when a user account exists but has no roles assigned, login fails.
  /// This prevents authentication for accounts in an invalid state.
  /// </summary>
  [Fact]
  public async Task LoginAsync_WithNoRoles_ReturnsFailure()
  {
    // Arrange
    var loginDto = new LoginDTO
    {
      Email = "user@example.com",
      Password = "Password123!"
    };

    var user = new ApiUser { Id = "user123", Email = loginDto.Email };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(loginDto.Email))
        .ReturnsAsync(user);

    _mockAuthRepo.Setup(x => x.CheckPasswordAsync(user, loginDto.Password))
        .ReturnsAsync(true);

    _mockAuthRepo.Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(new List<string>()); // Empty roles

    // Act
    var result = await _authService.LoginAsync(loginDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Invalid email or password");
    _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApiUser>(), It.IsAny<List<string>>()), Times.Never);
  }

  /// <summary>
  /// Tests that when attempting to log in with an email that doesn't exist in the system, login fails with generic error message.
  /// Uses generic error message to avoid leaking information about which emails are registered.
  /// </summary>
  [Fact]
  public async Task LoginAsync_WithNonExistentEmail_ReturnsFailure()
  {
    // Arrange
    var loginDto = new LoginDTO
    {
      Email = "nonexistent@example.com",
      Password = "Password123!"
    };

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(loginDto.Email))
        .ReturnsAsync((ApiUser?)null);

    // Act
    var result = await _authService.LoginAsync(loginDto);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Invalid email or password");
    _mockAuthRepo.Verify(x => x.CheckPasswordAsync(It.IsAny<ApiUser>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region GoogleLoginAsync Tests

  /// <summary>
  /// Tests successful Google OAuth login for an existing user: finds user by email, gets roles, generates token and sets cookie.
  /// This is the typical flow when a user who previously logged in with Google signs in again.
  /// </summary>
  [Fact]
  public async Task GoogleLoginAsync_WithExistingUser_ReturnsSuccessWithUserInfo()
  {
    // Arrange
    var email = "user@gmail.com";
    var googleId = "google123";
    var fullName = "Google User";

    var user = new ApiUser
    {
      Id = "user123",
      Email = email,
      FullName = fullName
    };

    var roles = new List<string> { "premium" };
    var expectedToken = "generated.jwt.token";

    var apiUserDto = new ApiUserDto
    {
      Email = email,
      FullName = fullName
    };

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, googleId),
            new Claim(ClaimTypes.Name, fullName)
        };

    var identity = new ClaimsIdentity(claims);
    var principal = new ClaimsPrincipal(identity);
    var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(email))
        .ReturnsAsync(user);

    _mockAuthRepo.Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(roles);

    _mockTokenService.Setup(x => x.GenerateToken(user, roles))
        .Returns(expectedToken);

    _mockMapper.Setup(x => x.Map<ApiUserDto>(user))
        .Returns(apiUserDto);

    // Act
    var result = await _authService.GoogleLoginAsync(authResult);

    // Assert
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Login successful");
    result.UserInfo.Should().NotBeNull();
    result.UserInfo!.Roles.Should().BeEquivalentTo(roles);

    _mockCookieService.Verify(x => x.SetAuthCookie(expectedToken), Times.Once);
    _mockAuthRepo.Verify(x => x.CreateAsync(It.IsAny<ApiUser>(), It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Tests first-time Google OAuth login: creates new user account with "premium" role and completes authentication.
  /// When no existing user is found with the email, a new account is automatically created.
  /// </summary>
  [Fact]
  public async Task GoogleLoginAsync_WithNewUser_CreatesUserAndReturnsSuccess()
  {
    // Arrange
    var email = "newuser@gmail.com";
    var googleId = "google456";
    var fullName = "New Google User";

    var roles = new List<string> { "premium" };
    var expectedToken = "generated.jwt.token";

    var apiUserDto = new ApiUserDto
    {
      Email = email,
      FullName = fullName
    };

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, googleId),
            new Claim(ClaimTypes.Name, fullName)
        };

    var identity = new ClaimsIdentity(claims);
    var principal = new ClaimsPrincipal(identity);
    var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(email))
        .ReturnsAsync((ApiUser?)null);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), It.IsAny<string>()))
        .ReturnsAsync(IdentityResult.Success);

    _mockAuthRepo.Setup(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"))
        .ReturnsAsync(IdentityResult.Success);

    _mockAuthRepo.Setup(x => x.GetRolesAsync(It.IsAny<ApiUser>()))
        .ReturnsAsync(roles);

    _mockTokenService.Setup(x => x.GenerateToken(It.IsAny<ApiUser>(), roles))
        .Returns(expectedToken);

    _mockMapper.Setup(x => x.Map<ApiUserDto>(It.IsAny<ApiUser>()))
        .Returns(apiUserDto);

    // Act
    var result = await _authService.GoogleLoginAsync(authResult);

    // Assert
    result.Success.Should().BeTrue();
    result.Message.Should().Be("Login successful");

    _mockAuthRepo.Verify(x => x.CreateAsync(
        It.Is<ApiUser>(u =>
            u.Email == email &&
            u.FullName == fullName &&
            u.EmailConfirmed == true &&
            u.UserName == email),
        It.IsAny<string>()), Times.Once);

    _mockAuthRepo.Verify(x => x.AddToRoleAsync(It.IsAny<ApiUser>(), "premium"), Times.Once);
    _mockEmailListCacheService.Verify(x => x.ToggleUserFromNewsletterAsync(
        It.Is<EmailNewsletterDTO>(dto => dto.Email == email)), Times.Once);
  }

  /// <summary>
  /// Tests that when Google authentication fails at the external provider level, the service returns appropriate failure response.
  /// </summary>
  [Fact]
  public async Task GoogleLoginAsync_WhenAuthenticationFails_ReturnsFailure()
  {
    // Arrange
    var authResult = AuthenticateResult.Fail("Google authentication failed");

    // Act
    var result = await _authService.GoogleLoginAsync(authResult);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Google authentication failed");
    _mockAuthRepo.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Tests that when user creation fails during Google OAuth, the service returns appropriate failure response.
  /// </summary>
  [Fact]
  public async Task GoogleLoginAsync_WhenUserCreationFails_ReturnsFailure()
  {
    // Arrange
    var email = "user@gmail.com";
    var googleId = "google789";
    var fullName = "Test User";

    var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, googleId),
            new Claim(ClaimTypes.Name, fullName)
        };

    var identity = new ClaimsIdentity(claims);
    var principal = new ClaimsPrincipal(identity);
    var authResult = AuthenticateResult.Success(new AuthenticationTicket(principal, "Google"));

    _mockAuthRepo.Setup(x => x.FindByEmailAsync(email))
        .ReturnsAsync((ApiUser?)null);

    _mockAuthRepo.Setup(x => x.CreateAsync(It.IsAny<ApiUser>(), It.IsAny<string>()))
        .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Creation failed" }));

    // Act
    var result = await _authService.GoogleLoginAsync(authResult);

    // Assert
    result.Success.Should().BeFalse();
    result.Message.Should().Be("Failed to create user account");
    _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApiUser>(), It.IsAny<List<string>>()), Times.Never);
  }

  #endregion
}
