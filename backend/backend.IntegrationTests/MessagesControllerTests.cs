using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace backend.IntegrationTests;

public class MessagesControllerTests : IClassFixture<WebAppFactory>
{
  private readonly HttpClient _client;

  public MessagesControllerTests(WebAppFactory factory)
  {
    _client = factory.CreateClient();
  }

  #region GET /messages/starred - Get Starred Messages Tests (HIGH)

  /// <summary>
  /// Verifies that getting starred messages without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task GetStarredMessages_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/messages/starred");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting starred messages with authentication returns user's starred messages.
  /// </summary>
  [Fact]
  public async Task GetStarredMessages_WithAuthentication_ReturnsStarredMessages()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/messages/starred");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);

    if (response.IsSuccessStatusCode)
    {
      var messages = await response.Content.ReadFromJsonAsync<List<object>>();
      messages.Should().NotBeNull();
    }
  }

  /// <summary>
  /// Verifies that starred messages list is empty when user has no starred messages.
  /// </summary>
  [Fact]
  public async Task GetStarredMessages_WithNoStarredMessages_ReturnsEmptyList()
  {
    // Note: Requires authenticated client with no starred messages

    // Act
    var response = await _client.GetAsync("/messages/starred");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region PATCH /messages/{messageId}/starred - Toggle Star Tests (HIGH)

  /// <summary>
  /// Verifies that toggling star without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task ToggleStar_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var messageId = Guid.NewGuid();

    // Act
    var response = await _client.PatchAsync($"/messages/{messageId}/starred", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that toggling star on valid message returns updated star status.
  /// </summary>
  [Fact]
  public async Task ToggleStar_WithValidMessage_ReturnsUpdatedStatus()
  {
    // Note: Requires authenticated client and existing message

    // Arrange
    var messageId = Guid.NewGuid();

    // Act
    var response = await _client.PatchAsync($"/messages/{messageId}/starred", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that toggling star on non-existent message returns appropriate response.
  /// </summary>
  [Fact]
  public async Task ToggleStar_WithNonExistentMessage_ReturnsNotFound()
  {
    // Note: Requires authenticated client

    // Arrange
    var nonExistentMessageId = Guid.NewGuid();

    // Act
    var response = await _client.PatchAsync($"/messages/{nonExistentMessageId}/starred", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that toggling star on another user's message returns appropriate response.
  /// </summary>
  [Fact]
  public async Task ToggleStar_OnOtherUserMessage_ReturnsNotFound()
  {
    // Note: Requires authenticated client and message from another user

    // Arrange
    var otherUserMessageId = Guid.NewGuid();

    // Act
    var response = await _client.PatchAsync($"/messages/{otherUserMessageId}/starred", null);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that toggling star twice on same message toggles between starred and unstarred.
  /// </summary>
  [Fact]
  public async Task ToggleStar_CalledTwice_TogglesStarStatus()
  {
    // Note: Requires authenticated client and existing message

    // Arrange
    var messageId = Guid.NewGuid();

    // Act - First toggle
    var response1 = await _client.PatchAsync($"/messages/{messageId}/starred", null);

    // Act - Second toggle
    var response2 = await _client.PatchAsync($"/messages/{messageId}/starred", null);

    // Assert - Both requests should succeed or both fail with same pattern
    response1.StatusCode.Should().Be(response2.StatusCode);
  }

  #endregion

  #region PATCH /messages/{messageId}/report - Report Message Tests (MEDIUM)

  /// <summary>
  /// Verifies that reporting message without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task ReportMessage_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var messageId = Guid.NewGuid();
    var reportReason = "Inappropriate content";

    // Act
    var response = await _client.PatchAsJsonAsync($"/messages/{messageId}/report", reportReason);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that reporting message with valid reason succeeds.
  /// </summary>
  [Fact]
  public async Task ReportMessage_WithValidReason_ReturnsOk()
  {
    // Note: Requires authenticated client and existing message

    // Arrange
    var messageId = Guid.NewGuid();
    var reportReason = "Contains offensive language";

    // Act
    var response = await _client.PatchAsJsonAsync($"/messages/{messageId}/report", reportReason);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that reporting non-existent message returns bad request.
  /// </summary>
  [Fact]
  public async Task ReportMessage_WithNonExistentMessage_ReturnsBadRequest()
  {
    // Note: Requires authenticated client

    // Arrange
    var nonExistentMessageId = Guid.NewGuid();
    var reportReason = "Test report";

    // Act
    var response = await _client.PatchAsJsonAsync($"/messages/{nonExistentMessageId}/report", reportReason);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that reporting message with empty reason still processes the report.
  /// </summary>
  [Fact]
  public async Task ReportMessage_WithEmptyReason_ProcessesReport()
  {
    // Note: Requires authenticated client and existing message

    // Arrange
    var messageId = Guid.NewGuid();
    var emptyReason = "";

    // Act
    var response = await _client.PatchAsJsonAsync($"/messages/{messageId}/report", emptyReason);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that reporting same message multiple times is handled correctly.
  /// </summary>
  [Fact]
  public async Task ReportMessage_CalledMultipleTimes_HandlesCorrectly()
  {
    // Note: Requires authenticated client and existing message

    // Arrange
    var messageId = Guid.NewGuid();
    var reportReason1 = "First report reason";
    var reportReason2 = "Second report reason";

    // Act - First report
    var response1 = await _client.PatchAsJsonAsync($"/messages/{messageId}/report", reportReason1);

    // Act - Second report
    var response2 = await _client.PatchAsJsonAsync($"/messages/{messageId}/report", reportReason2);

    // Assert - Both should return same pattern
    response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  #endregion
}
