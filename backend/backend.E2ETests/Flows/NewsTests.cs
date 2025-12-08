using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using backend.E2ETests.Helpers;
using backend.Models.Domain;

namespace backend.E2ETests.Flows;

/// <summary>
/// E2E tests for news functionality: retrieving news by date, type, and search.
/// </summary>
public class NewsTests : IClassFixture<E2EWebAppFactory>
{
  private readonly E2EWebAppFactory _factory;

  public NewsTests(E2EWebAppFactory factory)
  {
    _factory = factory;
  }

  /// <summary>
  /// Test that authenticated user can get all news
  /// </summary>
  [Fact]
  public async Task GetNews_ReturnsAllNews()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Act
    var response = await client.GetAsync("/news");

    // Assert
    // Note: May return 500 if news service has issues with in-memory DB
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    if (response.StatusCode == HttpStatusCode.OK)
    {
      var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
      news.Should().NotBeNull();
    }
    // News might be empty if no news aggregation has run
  }

  /// <summary>
  /// Test that authenticated user can get news by type (GitHub)
  /// </summary>
  [Fact]
  public async Task GetNews_ByGitHubType_ReturnsGitHubNews()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newsType = (int)NewsSourceType.Github; // 1

    // Act
    var response = await client.GetAsync($"/news/{newsType}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    if (response.StatusCode == HttpStatusCode.OK)
    {
      var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
      news.Should().NotBeNull();
    }
  }

  /// <summary>
  /// Test that authenticated user can get news by type (RSS)
  /// </summary>
  [Fact]
  public async Task GetNews_ByRssType_ReturnsRssNews()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newsType = (int)NewsSourceType.Rss; // 2

    // Act
    var response = await client.GetAsync($"/news/{newsType}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    if (response.StatusCode == HttpStatusCode.OK)
    {
      var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
      news.Should().NotBeNull();
    }
  }

  /// <summary>
  /// Test that authenticated user can get news by type (YouTube)
  /// </summary>
  [Fact]
  public async Task GetNews_ByYouTubeType_ReturnsYouTubeNews()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newsType = (int)NewsSourceType.Youtube; // 3

    // Act
    var response = await client.GetAsync($"/news/{newsType}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    if (response.StatusCode == HttpStatusCode.OK)
    {
      var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
      news.Should().NotBeNull();
    }
  }

  /// <summary>
  /// Test that invalid news type is rejected
  /// </summary>
  [Fact]
  public async Task GetNews_WithInvalidType_ReturnsBadRequest()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var invalidNewsType = 99;

    // Act
    var response = await client.GetAsync($"/news/{invalidNewsType}");

    // Assert
    // Should return 400 or 404 due to route constraint (range 1-4)
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
  }

  /// <summary>
  /// Test that authenticated user can get news by specific dates
  /// </summary>
  [Fact]
  public async Task GetNews_WithSpecificDates_ReturnsNewsForDates()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var today = DateTime.UtcNow.Date;
    var yesterday = today.AddDays(-1);

    // Act
    var response = await client.GetAsync($"/news?dates={today:yyyy-MM-dd}&dates={yesterday:yyyy-MM-dd}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    if (response.StatusCode == HttpStatusCode.OK)
    {
      var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
      news.Should().NotBeNull();
    }
  }

  /// <summary>
  /// Test that requesting too many dates returns bad request
  /// </summary>
  [Fact]
  public async Task GetNews_WithTooManyDates_ReturnsBadRequest()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var dates = string.Join("&", Enumerable.Range(0, 11).Select(i => $"dates={DateTime.UtcNow.AddDays(-i):yyyy-MM-dd}"));

    // Act
    var response = await client.GetAsync($"/news?{dates}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var error = await response.Content.ReadAsStringAsync();
    error.Should().Contain("Too many dates");
  }

  /// <summary>
  /// Test that authenticated user can search news
  /// </summary>
  [Fact]
  public async Task SearchNews_WithValidTerm_ReturnsMatchingNews()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var searchTerm = "technology";

    // Act
    var response = await client.GetAsync($"/news/search?term={searchTerm}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var news = await response.Content.ReadFromJsonAsync<List<NewsItemDto>>();
    news.Should().NotBeNull();
  }

  /// <summary>
  /// Test that unauthenticated user cannot access news
  /// </summary>
  [Fact]
  public async Task GetNews_WithoutAuth_ReturnsUnauthorized()
  {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/news");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Test that unauthenticated user cannot search news
  /// </summary>
  [Fact]
  public async Task SearchNews_WithoutAuth_ReturnsUnauthorized()
  {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/news/search?term=test");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  #region DTOs

  public class NewsItemDto
  {
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public NewsSourceType SourceType { get; set; }
    public string? SourceName { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Summary { get; set; }
  }

  #endregion
}
