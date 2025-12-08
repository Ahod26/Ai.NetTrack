using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using OpenAI.Embeddings;
using backend.Services.Classes.Cache;
using backend.Repository.Interfaces;
using backend.Models.Configuration;
using backend.Models.Domain;
using System.ClientModel;

namespace backend.UnitTests.Services.Cache;

public class LLMCacheServiceTests
{
  private readonly EmbeddingClient _embeddingClient;
  private readonly Mock<ILLMCacheRepo> _mockLLMCacheRepo;
  private readonly Mock<ILogger<LLMCacheService>> _mockLogger;
  private readonly LLMCacheSettings _settings;
  private readonly LLMCacheService _service;

  public LLMCacheServiceTests()
  {
    // Load configuration from user secrets and environment variables to get OpenAI API key
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<LLMCacheServiceTests>()
        .AddEnvironmentVariables()
        .Build();

    var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
      throw new InvalidOperationException(
          "OpenAI API key not found. Please set it in user secrets (OpenAI:ApiKey) or OPENAI_API_KEY environment variable.");
    }
    _embeddingClient = new EmbeddingClient("text-embedding-3-small", apiKey);

    _mockLLMCacheRepo = new Mock<ILLMCacheRepo>();
    _mockLogger = new Mock<ILogger<LLMCacheService>>();

    _settings = new LLMCacheSettings
    {
      MaxCacheableMessageCountSemantic = 8,
      MaxCacheableMessageCountExact = 2,
      SemanticSimilarityThreshold = 0.85f,
      BaseCacheLifetimeDays = 21,
      CacheLifetimeDecayFactor = 0.7
    };

    var options = Options.Create(_settings);

    // Setup InitializeRedisIndexAsync to complete without error
    _mockLLMCacheRepo.Setup(x => x.InitializeRedisIndexAsync()).Returns(Task.CompletedTask);

    _service = new LLMCacheService(
        _embeddingClient,
        _mockLLMCacheRepo.Object,
        options,
        _mockLogger.Object
    );
  }

  #region SetCachedResponseAsync Tests

  /// <summary>
  /// Verifies that when context count is within exact cache limit, exact cache is stored.
  /// Note: Semantic cache uses OpenAI SDK which cannot be mocked (sealed classes).
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_ContextWithinExactLimit_StoresExactCache()
  {
    // Arrange
    var userMessage = "What is C#?";
    var context = CreateChatMessages(1); // Within MaxCacheableMessageCountExact (2)
    var response = "C# is a programming language.";

    // Act
    await _service.SetCachedResponseAsync(userMessage, context, response);

    // Assert - Can only verify exact cache since OpenAI SDK is not mockable
    _mockLLMCacheRepo.Verify(x => x.StoreExactCacheAsync(
        It.IsAny<string>(),
        response,
        It.IsAny<TimeSpan>(),
        false), Times.Once);
  }

  /// <summary>
  /// Verifies that when context count exceeds exact limit but is within semantic limit, only semantic cache is used.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_ContextAboveExactButWithinSemantic_StoresOnlySemantic()
  {
    // Arrange
    var userMessage = "Explain async/await";
    var context = CreateChatMessages(5); // Above exact (2) but within semantic (8)
    var response = "Async/await is for asynchronous programming.";

    // Act
    await _service.SetCachedResponseAsync(userMessage, context, response);

    // Assert
    _mockLLMCacheRepo.Verify(x => x.StoreExactCacheAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<TimeSpan>(),
        false), Times.Never);

    _mockLLMCacheRepo.Verify(x => x.StoreSemanticCacheAsync(
        response,
        It.IsAny<byte[]>(),
        context.Count,
        It.IsAny<List<string>>(),
        It.IsAny<TimeSpan>(),
        It.IsAny<string>()), Times.Once);
  }

  /// <summary>
  /// Verifies that when context count exceeds semantic limit, caching is skipped entirely.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_ContextAboveSemanticLimit_SkipsCaching()
  {
    // Arrange
    var userMessage = "Long conversation";
    var context = CreateChatMessages(10); // Above MaxCacheableMessageCountSemantic (8)
    var response = "Response to long conversation.";

    // Act
    await _service.SetCachedResponseAsync(userMessage, context, response);

    // Assert
    _mockLLMCacheRepo.Verify(x => x.StoreExactCacheAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<TimeSpan>(),
        It.IsAny<bool>()), Times.Never);

    _mockLLMCacheRepo.Verify(x => x.StoreSemanticCacheAsync(
        It.IsAny<string>(),
        It.IsAny<byte[]>(),
        It.IsAny<int>(),
        It.IsAny<List<string>>(),
        It.IsAny<TimeSpan>(),
        It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Verifies that expiration time decreases as message count increases (exponential decay).
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_CalculatesExpirationCorrectly()
  {
    // Arrange
    var userMessage = "Test message";
    var context1 = CreateChatMessages(1);
    var context5 = CreateChatMessages(5);
    var response = "Test response";

    TimeSpan? expiration1 = null;
    TimeSpan? expiration5 = null;

    _mockLLMCacheRepo.Setup(x => x.StoreSemanticCacheAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<int>(),
            It.IsAny<List<string>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<string>()))
        .Callback<string, byte[], int, List<string>, TimeSpan, string>(
            (r, e, mc, t, exp, c) =>
            {
              if (mc == 1) expiration1 = exp;
              if (mc == 5) expiration5 = exp;
            })
        .Returns(Task.CompletedTask);

    // Act
    await _service.SetCachedResponseAsync(userMessage, context1, response);
    await _service.SetCachedResponseAsync(userMessage, context5, response);

    // Assert
    expiration1.Should().NotBeNull();
    expiration5.Should().NotBeNull();
    expiration1!.Value.TotalDays.Should().BeGreaterThan(expiration5!.Value.TotalDays);
    expiration1.Value.TotalDays.Should().BeApproximately(21, 1); // Base = 21 days
    expiration5.Value.TotalDays.Should().BeLessThan(10); // Should be ~5 days
  }

  /// <summary>
  /// Verifies that exceptions during caching are logged but don't throw.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_WhenExceptionThrown_LogsErrorAndDoesNotThrow()
  {
    // Arrange
    var userMessage = "Test";
    var context = CreateChatMessages(1);
    var response = "Response";
    var exception = new Exception("Redis connection failed");

    _mockLLMCacheRepo.Setup(x => x.StoreExactCacheAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<bool>()))
        .ThrowsAsync(exception);

    // Act
    Func<Task> act = async () => await _service.SetCachedResponseAsync(userMessage, context, response);

    // Assert
    await act.Should().NotThrowAsync();
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in SetCachedResponseAsync")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  /// <summary>
  /// Verifies that embedding generation is called with the correct context string.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseAsync_CallsEmbeddingGenerationWithCorrectContext()
  {
    // Arrange
    var userMessage = "What is ASP.NET?";
    var context = CreateChatMessages(2);
    var response = "ASP.NET is a web framework.";

    // Act
    await _service.SetCachedResponseAsync(userMessage, context, response);

    // Assert - Verify StoreSemanticCacheAsync was called (proves embedding was generated)
    _mockLLMCacheRepo.Verify(x => x.StoreSemanticCacheAsync(
        response,
        It.IsAny<byte[]>(),
        context.Count,
        It.IsAny<List<string>>(),
        It.IsAny<TimeSpan>(),
        It.Is<string>(s => s.Contains("User:") && s.Contains("Assistant:") && s.Contains(userMessage))),
        Times.Once);
  }

  #endregion

  #region GetCachedResponseAsync Tests

  /// <summary>
  /// Verifies that exact cache hit returns cached response immediately without semantic search.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_ExactCacheHit_ReturnsImmediatelyWithoutSemanticSearch()
  {
    // Arrange
    var userMessage = "What is C#?";
    var context = CreateChatMessages(1);
    var cachedResponse = "C# is a programming language.";

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ReturnsAsync(cachedResponse);

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().Be(cachedResponse);
    _mockLLMCacheRepo.Verify(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false), Times.Once);
    _mockLLMCacheRepo.Verify(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never);
  }

  /// <summary>
  /// Verifies that when exact cache misses but semantic cache hits, semantic response is returned.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_ExactMissSemanticHit_ReturnsSemanticMatch()
  {
    // Arrange
    var userMessage = "Explain C#";
    var context = CreateChatMessages(2);
    var semanticResponse = "C# is a modern programming language.";

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ReturnsAsync((string?)null);

    // Semantic search returns high similarity match
    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(("doc123", 0.10f)); // Score of 0.10 = similarity of 0.90

    _mockLLMCacheRepo.Setup(x => x.GetResponseFromSemanticMatchAsync("doc123"))
        .ReturnsAsync(semanticResponse);

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().Be(semanticResponse);
    _mockLLMCacheRepo.Verify(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), context.Count), Times.Once);
    _mockLLMCacheRepo.Verify(x => x.GetResponseFromSemanticMatchAsync("doc123"), Times.Once);
  }

  /// <summary>
  /// Verifies that when both exact and semantic caches miss, null is returned.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_BothCachesMiss_ReturnsNull()
  {
    // Arrange
    var userMessage = "New question";
    var context = CreateChatMessages(1);

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ReturnsAsync((string?)null);

    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(((string, float)?)null);

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Verifies that when context exceeds semantic limit, null is returned without searching.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_ContextAboveSemanticLimit_ReturnsNullWithoutSearch()
  {
    // Arrange
    var userMessage = "Long conversation";
    var context = CreateChatMessages(10); // Above limit

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().BeNull();
    _mockLLMCacheRepo.Verify(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    _mockLLMCacheRepo.Verify(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never);
  }

  /// <summary>
  /// Verifies that semantic similarity below threshold returns null.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_SemanticSimilarityBelowThreshold_ReturnsNull()
  {
    // Arrange
    var userMessage = "Similar but not exact";
    var context = CreateChatMessages(1);

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ReturnsAsync((string?)null);

    // Score of 0.20 = similarity of 0.80 (below threshold of 0.85)
    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(("doc123", 0.20f));

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().BeNull();
    _mockLLMCacheRepo.Verify(x => x.GetResponseFromSemanticMatchAsync(It.IsAny<string>()), Times.Never);
  }

  /// <summary>
  /// Verifies that semantic similarity above threshold returns cached response.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_SemanticSimilarityAboveThreshold_ReturnsCachedResponse()
  {
    // Arrange
    var userMessage = "Very similar question";
    var context = CreateChatMessages(1);
    var cachedResponse = "Cached answer";

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ReturnsAsync((string?)null);

    // Score of 0.05 = similarity of 0.95 (above threshold of 0.85)
    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(("doc456", 0.05f));

    _mockLLMCacheRepo.Setup(x => x.GetResponseFromSemanticMatchAsync("doc456"))
        .ReturnsAsync(cachedResponse);

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().Be(cachedResponse);
  }

  /// <summary>
  /// Verifies that exceptions during retrieval are logged and null is returned.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseAsync_WhenExceptionThrown_LogsErrorAndReturnsNull()
  {
    // Arrange
    var userMessage = "Test";
    var context = CreateChatMessages(1);
    var exception = new Exception("Cache retrieval failed");

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .ThrowsAsync(exception);

    // Act
    var result = await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    result.Should().BeNull();
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in GetCachedResponseAsync")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region GetCachedResponseForNewsResourceAsync Tests

  /// <summary>
  /// Verifies that news resource cache hit returns cached response.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseForNewsResourceAsync_CacheHit_ReturnsCachedResponse()
  {
    // Arrange
    var url = "https://example.com/news/article1";
    var cachedResponse = "News article summary";

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(url, true))
        .ReturnsAsync(cachedResponse);

    // Act
    var result = await _service.GetCachedResponseForNewsResourceAsync(url);

    // Assert
    result.Should().Be(cachedResponse);
    _mockLLMCacheRepo.Verify(x => x.GetExactCachedResponseAsync(url, true), Times.Once);
  }

  /// <summary>
  /// Verifies that news resource cache miss returns null.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseForNewsResourceAsync_CacheMiss_ReturnsNull()
  {
    // Arrange
    var url = "https://example.com/news/article2";

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(url, true))
        .ReturnsAsync((string?)null);

    // Act
    var result = await _service.GetCachedResponseForNewsResourceAsync(url);

    // Assert
    result.Should().BeNull();
  }

  /// <summary>
  /// Verifies that exceptions during news resource retrieval are logged and null is returned.
  /// </summary>
  [Fact]
  public async Task GetCachedResponseForNewsResourceAsync_WhenExceptionThrown_LogsErrorAndReturnsNull()
  {
    // Arrange
    var url = "https://example.com/news/article3";
    var exception = new Exception("Cache error");

    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(url, true))
        .ThrowsAsync(exception);

    // Act
    var result = await _service.GetCachedResponseForNewsResourceAsync(url);

    // Assert
    result.Should().BeNull();
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in GetCachedResponseForNewsResourceAsync")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region SetCachedResponseForNewsResourceAsync Tests

  /// <summary>
  /// Verifies that news resource response is stored with 2-day expiration.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseForNewsResourceAsync_StoresWithTwoDayExpiration()
  {
    // Arrange
    var url = "https://example.com/news/article1";
    var response = "News summary response";

    TimeSpan? capturedExpiration = null;
    _mockLLMCacheRepo.Setup(x => x.StoreExactCacheAsync(
            url,
            response,
            It.IsAny<TimeSpan>(),
            true))
        .Callback<string, string, TimeSpan, bool>((u, r, exp, isUrl) => capturedExpiration = exp)
        .Returns(Task.CompletedTask);

    // Act
    await _service.SetCachedResponseForNewsResourceAsync(url, response);

    // Assert
    _mockLLMCacheRepo.Verify(x => x.StoreExactCacheAsync(url, response, It.IsAny<TimeSpan>(), true), Times.Once);
    capturedExpiration.Should().NotBeNull();
    capturedExpiration!.Value.TotalDays.Should().BeApproximately(2, 0.1);
  }

  /// <summary>
  /// Verifies that exceptions during news resource storage are logged but don't throw.
  /// </summary>
  [Fact]
  public async Task SetCachedResponseForNewsResourceAsync_WhenExceptionThrown_LogsErrorAndDoesNotThrow()
  {
    // Arrange
    var url = "https://example.com/news/article2";
    var response = "News response";
    var exception = new Exception("Storage failed");

    _mockLLMCacheRepo.Setup(x => x.StoreExactCacheAsync(url, response, It.IsAny<TimeSpan>(), true))
        .ThrowsAsync(exception);

    // Act
    Func<Task> act = async () => await _service.SetCachedResponseForNewsResourceAsync(url, response);

    // Assert
    await act.Should().NotThrowAsync();
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error in SetCachedResponseForNewsResourceAsync")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region Cache Key Generation Tests (Through Public Interface)

  /// <summary>
  /// Verifies that same message and context generate the same cache key (deterministic).
  /// </summary>
  [Fact]
  public async Task CacheKey_SameMessageAndContext_GeneratesSameKey()
  {
    // Arrange
    var userMessage = "What is C#?";
    var context = CreateChatMessages(1);

    var capturedKeys = new List<string>();
    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .Callback<string, bool>((key, isUrl) => capturedKeys.Add(key))
        .ReturnsAsync((string?)null);

    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(((string, float)?)null);

    // Act
    await _service.GetCachedResponseAsync(userMessage, context);
    await _service.GetCachedResponseAsync(userMessage, context);

    // Assert
    capturedKeys.Should().HaveCount(2);
    capturedKeys[0].Should().Be(capturedKeys[1]);
    capturedKeys[0].Should().NotBeNullOrEmpty();
  }

  /// <summary>
  /// Verifies that different messages generate different cache keys.
  /// </summary>
  [Fact]
  public async Task CacheKey_DifferentMessage_GeneratesDifferentKey()
  {
    // Arrange
    var message1 = "What is C#?";
    var message2 = "What is Python?";
    var context = CreateChatMessages(1);

    var capturedKeys = new List<string>();
    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .Callback<string, bool>((key, isUrl) => capturedKeys.Add(key))
        .ReturnsAsync((string?)null);

    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(((string, float)?)null);

    // Act
    await _service.GetCachedResponseAsync(message1, context);
    await _service.GetCachedResponseAsync(message2, context);

    // Assert
    capturedKeys.Should().HaveCount(2);
    capturedKeys[0].Should().NotBe(capturedKeys[1]);
  }

  /// <summary>
  /// Verifies that different context orders generate different cache keys (order matters).
  /// </summary>
  [Fact]
  public async Task CacheKey_DifferentContextOrder_GeneratesDifferentKey()
  {
    // Arrange
    var userMessage = "Test";
    var context1 = new List<ChatMessage>
    {
      new ChatMessage { Content = "First", Type = MessageType.User },
      new ChatMessage { Content = "Second", Type = MessageType.Assistant }
    };
    var context2 = new List<ChatMessage>
    {
      new ChatMessage { Content = "Second", Type = MessageType.Assistant },
      new ChatMessage { Content = "First", Type = MessageType.User }
    };

    var capturedKeys = new List<string>();
    _mockLLMCacheRepo.Setup(x => x.GetExactCachedResponseAsync(It.IsAny<string>(), false))
        .Callback<string, bool>((key, isUrl) => capturedKeys.Add(key))
        .ReturnsAsync((string?)null);

    _mockLLMCacheRepo.Setup(x => x.SearchSemanticCacheAsync(It.IsAny<byte[]>(), It.IsAny<int>()))
        .ReturnsAsync(((string, float)?)null);

    // Act
    await _service.GetCachedResponseAsync(userMessage, context1);
    await _service.GetCachedResponseAsync(userMessage, context2);

    // Assert
    capturedKeys.Should().HaveCount(2);
    capturedKeys[0].Should().NotBe(capturedKeys[1]);
  }

  #endregion

  #region TopicExtractor Tests

  /// <summary>
  /// Verifies that TopicExtractor extracts a single tech term from text.
  /// </summary>
  [Fact]
  public void TopicExtractor_SingleTechTerm_ExtractsCorrectly()
  {
    // Arrange
    var extractor = new TopicExtractor();
    var text = "I'm learning ASP.NET Core for web development.";

    // Act
    var topics = extractor.ExtractTopics(text);

    // Assert
    topics.Should().Contain("aspnet");
  }

  /// <summary>
  /// Verifies that TopicExtractor extracts multiple tech terms from text.
  /// </summary>
  [Fact]
  public void TopicExtractor_MultipleTechTerms_ExtractsAll()
  {
    // Arrange
    var extractor = new TopicExtractor();
    var text = "Using C# with ASP.NET and Redis for caching with OpenAI integration.";

    // Act
    var topics = extractor.ExtractTopics(text);

    // Assert
    topics.Should().Contain("csharp");
    topics.Should().Contain("aspnet");
    topics.Should().Contain("redis");
    topics.Should().Contain("openai");
    topics.Count.Should().BeGreaterThanOrEqualTo(4);
  }

  /// <summary>
  /// Verifies that TopicExtractor is case insensitive.
  /// </summary>
  [Fact]
  public void TopicExtractor_CaseInsensitive_ExtractsCorrectly()
  {
    // Arrange
    var extractor = new TopicExtractor();
    var text = "BLAZOR and blazor and Blazor are the same.";

    // Act
    var topics = extractor.ExtractTopics(text);

    // Assert
    topics.Should().Contain("blazor");
  }

  /// <summary>
  /// Verifies that TopicExtractor returns unique topics (no duplicates).
  /// </summary>
  [Fact]
  public void TopicExtractor_DuplicateTerms_ReturnsUnique()
  {
    // Arrange
    var extractor = new TopicExtractor();
    var text = "Redis Redis Redis for caching.";

    // Act
    var topics = extractor.ExtractTopics(text);

    // Assert
    topics.Should().ContainSingle("redis");
  }

  /// <summary>
  /// Verifies that TopicExtractor returns empty list when no tech terms found.
  /// </summary>
  [Fact]
  public void TopicExtractor_NoTechTerms_ReturnsEmptyList()
  {
    // Arrange
    var extractor = new TopicExtractor();
    var text = "This is just a normal conversation without any tech terms.";

    // Act
    var topics = extractor.ExtractTopics(text);

    // Assert
    topics.Should().BeEmpty();
  }

  #endregion

  #region Helper Methods

  private List<ChatMessage> CreateChatMessages(int count)
  {
    var messages = new List<ChatMessage>();
    for (int i = 0; i < count; i++)
    {
      messages.Add(new ChatMessage
      {
        Id = Guid.NewGuid(),
        Content = $"Message {i + 1}",
        Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
        CreatedAt = DateTime.UtcNow.AddMinutes(-i)
      });
    }
    return messages;
  }

  #endregion
}
