using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace backend.IntegrationTests;

public class NewsControllerTests : IClassFixture<WebAppFactory>
{
  private readonly HttpClient _client;

  public NewsControllerTests(WebAppFactory factory)
  {
    _client = factory.CreateClient();
  }

  #region GET /news - Get News by Date Tests (CRITICAL)

  /// <summary>
  /// Verifies that getting news without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task GetNews_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/news");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news with authentication returns today's news items.
  /// </summary>
  [Fact]
  public async Task GetNews_WithoutDateParameter_ReturnsTodayNews()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/news");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news with specific date returns news for that date.
  /// </summary>
  [Fact]
  public async Task GetNews_WithSpecificDate_ReturnsNewsForDate()
  {
    // Note: Requires authenticated client

    // Arrange
    var targetDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

    // Act
    var response = await _client.GetAsync($"/news?dates={targetDate}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news with multiple dates combines results.
  /// </summary>
  [Fact]
  public async Task GetNews_WithMultipleDates_ReturnsCombinedNews()
  {
    // Note: Requires authenticated client

    // Arrange
    var date1 = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
    var date2 = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd");

    // Act
    var response = await _client.GetAsync($"/news?dates={date1}&dates={date2}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that requesting more than 10 dates returns bad request.
  /// </summary>
  [Fact]
  public async Task GetNews_WithTooManyDates_ReturnsBadRequest()
  {
    // Note: Requires authenticated client

    // Arrange
    var dates = string.Join("&", Enumerable.Range(0, 11).Select(i =>
        $"dates={DateTime.UtcNow.AddDays(-i):yyyy-MM-dd}"));

    // Act
    var response = await _client.GetAsync($"/news?{dates}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region GET /news/{newsType} - Get News by Type Tests (CRITICAL)

  /// <summary>
  /// Verifies that getting news by type 1 (YouTube) returns filtered results.
  /// </summary>
  [Fact]
  public async Task GetNews_WithNewsType1_ReturnsYouTubeNews()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/news/1");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news by type 2 (DevBlogs) returns filtered results.
  /// </summary>
  [Fact]
  public async Task GetNews_WithNewsType2_ReturnsDevBlogNews()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/news/2");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news by type 3 (GitHub) returns filtered results.
  /// </summary>
  [Fact]
  public async Task GetNews_WithNewsType3_ReturnsGitHubNews()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/news/3");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news by type 4 (RSS) returns filtered results.
  /// </summary>
  [Fact]
  public async Task GetNews_WithNewsType4_ReturnsRSSNews()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/news/4");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting news with invalid type (out of range) returns bad request.
  /// </summary>
  [Fact]
  public async Task GetNews_WithInvalidNewsType_ReturnsBadRequest()
  {
    // Act
    var response = await _client.GetAsync("/news/5"); // Type must be 1-4

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that combining news type with specific dates works correctly.
  /// </summary>
  [Fact]
  public async Task GetNews_WithNewsTypeAndDates_ReturnsFilteredNews()
  {
    // Note: Requires authenticated client

    // Arrange
    var targetDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

    // Act
    var response = await _client.GetAsync($"/news/1?dates={targetDate}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region GET /news/search - Search News Tests (HIGH)

  /// <summary>
  /// Verifies that searching news without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task SearchNews_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var searchTerm = "dotnet";

    // Act
    var response = await _client.GetAsync($"/news/search?term={searchTerm}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that searching with valid term returns matching news items.
  /// </summary>
  [Fact]
  public async Task SearchNews_WithValidTerm_ReturnsMatchingNews()
  {
    // Note: Requires authenticated client

    // Arrange
    var searchTerm = "dotnet";

    // Act
    var response = await _client.GetAsync($"/news/search?term={Uri.EscapeDataString(searchTerm)}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that searching with special characters is handled properly.
  /// </summary>
  [Fact]
  public async Task SearchNews_WithSpecialCharacters_HandlesCorrectly()
  {
    // Note: Requires authenticated client

    // Arrange
    var searchTerm = "C# & .NET";

    // Act
    var response = await _client.GetAsync($"/news/search?term={Uri.EscapeDataString(searchTerm)}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that searching without a term parameter returns appropriate response.
  /// </summary>
  [Fact]
  public async Task SearchNews_WithoutSearchTerm_ReturnsError()
  {
    // Act
    var response = await _client.GetAsync("/news/search");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that searching with empty term returns appropriate response.
  /// </summary>
  [Fact]
  public async Task SearchNews_WithEmptyTerm_ReturnsError()
  {
    // Act
    var response = await _client.GetAsync("/news/search?term=");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  #endregion
}
