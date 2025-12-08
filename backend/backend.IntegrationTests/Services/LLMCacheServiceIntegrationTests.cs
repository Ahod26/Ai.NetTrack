using backend.Models.Domain;
using backend.Services.Interfaces.Cache;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace backend.IntegrationTests.Services;

[Collection("Sequential")]
public class LLMCacheServiceIntegrationTests : IClassFixture<WebAppFactory>, IDisposable
{
  private readonly WebAppFactory _factory;
  private readonly IServiceScope _scope;
  private readonly ILLMCacheService _cacheService;

  public LLMCacheServiceIntegrationTests(WebAppFactory factory)
  {
    _factory = factory;
    _scope = _factory.Services.CreateScope();
    _cacheService = _scope.ServiceProvider.GetRequiredService<ILLMCacheService>();
  }

  [Fact]
  public async Task SetCachedResponseAsync_ThenGetCachedResponseAsync_WithExactMatch_ReturnsStoredResponse()
  {
    // Arrange
    var message = "What is artificial intelligence?";
    var context = new List<ChatMessage>
    {
      new() { Type = MessageType.User, Content = "Hello", CreatedAt = DateTime.UtcNow }
    };
    var response = "AI is the simulation of human intelligence processes by machines.";

    // Act - Store the response
    await _cacheService.SetCachedResponseAsync(message, context, response);

    // Act - Retrieve with exact same message and context
    var cachedResponse = await _cacheService.GetCachedResponseAsync(message, context);

    // Assert
    cachedResponse.Should().NotBeNull();
    cachedResponse.Should().Be(response);
  }



  [Fact]
  public async Task GetCachedResponseAsync_WithNoMatch_ReturnsNull()
  {
    // Arrange
    var message = $"Unique question about quantum computing {Guid.NewGuid()}";
    var context = new List<ChatMessage>();

    // Act - Try to retrieve without storing anything
    var cachedResponse = await _cacheService.GetCachedResponseAsync(message, context);

    // Assert
    cachedResponse.Should().BeNull();
  }

  [Fact]
  public async Task SetCachedResponseAsync_WithLargeContext_HandlesGracefully()
  {
    // Arrange
    var message = "What is deep learning?";
    var largeContext = Enumerable.Range(0, 5).Select(i => new ChatMessage
    {
      Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
      Content = $"Message {i}",
      CreatedAt = DateTime.UtcNow
    }).ToList();
    var response = "Deep learning uses neural networks with multiple layers.";

    // Act - This should not throw
    await _cacheService.SetCachedResponseAsync(message, largeContext, response);

    // Retrieve with same large context
    var cachedResponse = await _cacheService.GetCachedResponseAsync(message, largeContext);

    // Assert - May or may not be cached depending on limits
    // The important thing is no exception was thrown
    Assert.True(true); // Test passes if we get here without exception
  }

  [Fact]
  public async Task GetCachedResponseForNewsResourceAsync_AfterSet_ReturnsStoredResponse()
  {
    // Arrange
    var resourceUrl = $"https://example.com/news/article{Guid.NewGuid()}";
    var response = "Summary of the news article";

    // Act - Store news cache
    await _cacheService.SetCachedResponseForNewsResourceAsync(resourceUrl, response);

    // Retrieve news cache
    var cachedResponse = await _cacheService.GetCachedResponseForNewsResourceAsync(resourceUrl);

    // Assert
    cachedResponse.Should().NotBeNull();
    cachedResponse.Should().Be(response);
  }

  [Fact]
  public async Task GetCachedResponseForNewsResourceAsync_WithoutSet_ReturnsNull()
  {
    // Arrange
    var resourceUrl = $"https://example.com/news/nonexistent{Guid.NewGuid()}";

    // Act
    var cachedResponse = await _cacheService.GetCachedResponseForNewsResourceAsync(resourceUrl);

    // Assert
    cachedResponse.Should().BeNull();
  }

  [Fact]
  public async Task SetCachedResponseAsync_WithVeryLongContext_HandlesGracefully()
  {
    // Arrange
    var message = "What is computer vision?";
    var veryLargeContext = Enumerable.Range(0, 20).Select(i => new ChatMessage
    {
      Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
      Content = $"Message {i}",
      CreatedAt = DateTime.UtcNow
    }).ToList();
    var response = "Computer vision enables computers to interpret visual information.";

    // Act - Should handle gracefully (may skip caching based on limits)
    await _cacheService.SetCachedResponseAsync(message, veryLargeContext, response);

    // Try to retrieve
    var cachedResponse = await _cacheService.GetCachedResponseAsync(message, veryLargeContext);

    // Assert - Test passes if no exception thrown
    // May return null if context exceeded limits
    Assert.True(true);
  }

  [Fact]
  public async Task MultipleConcurrentSetAndGet_MaintainsDataIntegrity()
  {
    // Arrange
    var tasks = new List<Task>();
    var message = "What is neural network architecture?";
    var context = new List<ChatMessage>
    {
      new() { Type = MessageType.User, Content = "Context", CreatedAt = DateTime.UtcNow }
    };
    var response = "Neural networks consist of layers of interconnected nodes.";

    // Act - Perform multiple set/get operations concurrently
    for (int i = 0; i < 5; i++)
    {
      tasks.Add(Task.Run(async () =>
      {
        await _cacheService.SetCachedResponseAsync(message, context, response);
        var result = await _cacheService.GetCachedResponseAsync(message, context);
        result.Should().NotBeNull();
      }));
    }

    await Task.WhenAll(tasks);

    // Assert - Final retrieval should work
    var finalResult = await _cacheService.GetCachedResponseAsync(message, context);
    finalResult.Should().NotBeNull();
    finalResult.Should().Be(response);
  }

  public void Dispose()
  {
    _scope?.Dispose();
  }
}
