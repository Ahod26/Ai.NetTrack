using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using backend.Services.Classes.ChatService;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.Cache;
using backend.Services.Interfaces.News;
using backend.Repository.Interfaces;
using backend.Models.Configuration;
using backend.Models.Dtos;
using backend.Models.Domain;
using ChatDomain = backend.Models.Domain.Chat;

namespace backend.UnitTests.Services.Chat;

public class ChatServiceTests
{
  private readonly Mock<IChatRepo> _mockChatRepo;
  private readonly Mock<IMessagesRepo> _mockMessagesRepo;
  private readonly Mock<IOpenAIService> _mockOpenAIService;
  private readonly Mock<IMapper> _mockMapper;
  private readonly Mock<ILLMCacheService> _mockLLMCacheService;
  private readonly Mock<IChatCacheService> _mockChatCacheService;
  private readonly Mock<INewsService> _mockNewsService;
  private readonly Mock<ILogger<ChatService>> _mockLogger;
  private readonly ChatService _chatService;
  private readonly StreamingSettings _streamingSettings;

  public ChatServiceTests()
  {
    _mockChatRepo = new Mock<IChatRepo>();
    _mockMessagesRepo = new Mock<IMessagesRepo>();
    _mockOpenAIService = new Mock<IOpenAIService>();
    _mockMapper = new Mock<IMapper>();
    _mockLLMCacheService = new Mock<ILLMCacheService>();
    _mockChatCacheService = new Mock<IChatCacheService>();
    _mockNewsService = new Mock<INewsService>();
    _mockLogger = new Mock<ILogger<ChatService>>();

    _streamingSettings = new StreamingSettings
    {
      ChunkSize = 5,
      DelayMs = 10
    };

    var options = Options.Create(_streamingSettings);

    _chatService = new ChatService(
        _mockChatRepo.Object,
        _mockMessagesRepo.Object,
        _mockOpenAIService.Object,
        _mockMapper.Object,
        _mockLLMCacheService.Object,
        _mockChatCacheService.Object,
        _mockNewsService.Object,
        _mockLogger.Object,
        options
    );
  }

  #region ProcessUserMessageAsync - Cache Hit Tests

  /// <summary>
  /// Verifies that when a regular chat has a cached response, it returns the cached content without calling OpenAI.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_RegularChatCacheHit_ReturnsStoredResponse()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "What is C#?";
    var cachedResponse = "C# is a programming language.";
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 5, isNewsRelated: false);
    var context = CreateChatMessages(3);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync(cachedResponse);

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, cachedResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(cachedResponse);
    _mockOpenAIService.Verify(x => x.GenerateResponseAsync(
        It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<CancellationToken>(),
        It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Func<string, Task>>(), It.IsAny<string>()),
        Times.Never);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseAsync(
        It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>()),
        Times.Never);
  }

  /// <summary>
  /// Verifies that for the initial message in a news-related chat, the news resource cache is checked and used if available.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_InitialNewsRelatedCacheHit_UsesNewsResourceCache()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Tell me about this news";
    var newsUrl = "https://example.com/news";
    var cachedResponse = "News content response";
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 0, isNewsRelated: true, newsUrl: newsUrl);
    var context = new List<ChatMessage>();

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseForNewsResourceAsync(newsUrl))
        .ReturnsAsync(cachedResponse);

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, cachedResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(cachedResponse);
    _mockLLMCacheService.Verify(x => x.GetCachedResponseForNewsResourceAsync(newsUrl), Times.Once);
    _mockLLMCacheService.Verify(x => x.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>()), Times.Never);
  }

  #endregion

  #region ProcessUserMessageAsync - Cache Miss Tests

  /// <summary>
  /// Verifies that when no cached response exists, OpenAI generates a new response and stores it in the cache.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_CacheMiss_GeneratesAndCachesResponse()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Explain microservices";
    var aiResponse = "Microservices are an architectural pattern.";
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 3, isNewsRelated: false);
    var context = CreateChatMessages(2);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync((string?)null);

    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, cancellationToken, false, false, It.IsAny<Func<string, Task>>(), null))
        .ReturnsAsync((aiResponse, 1000));

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, aiResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(aiResponse);
    _mockOpenAIService.Verify(x => x.GenerateResponseAsync(
        userMessage, context, cancellationToken, false, false, It.IsAny<Func<string, Task>>(), null),
        Times.Once);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseAsync(userMessage, context, aiResponse), Times.Once);
  }

  /// <summary>
  /// Verifies that the initial message in a news-related chat stores the response in the news resource cache.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_NewsRelatedInitial_StoresInNewsResourceCache()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Summarize this news";
    var newsUrl = "https://example.com/article";
    var newsContent = "News article content here";
    var aiResponse = "News summary response";
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 0, isNewsRelated: true, newsUrl: newsUrl, newsContent: newsContent);
    var context = new List<ChatMessage>();

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseForNewsResourceAsync(newsUrl))
        .ReturnsAsync((string?)null);

    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, cancellationToken, true, true, It.IsAny<Func<string, Task>>(), newsContent))
        .ReturnsAsync((aiResponse, 500));

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, aiResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(aiResponse);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseForNewsResourceAsync(newsUrl, aiResponse), Times.Once);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseAsync(It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region ProcessUserMessageAsync - Cancellation Tests

  /// <summary>
  /// Verifies that when message generation is cancelled, the partial response is not cached.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_CancelledDuringGeneration_DoesNotCache()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Long query";
    var partialResponse = "Partial response before cancellation";
    var cancellationTokenSource = new CancellationTokenSource();

    var chat = CreateMockChat(chatId, userId, messageCount: 2, isNewsRelated: false);
    var context = CreateChatMessages(1);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync((string?)null);

    // Simulate cancellation during generation
    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, It.IsAny<CancellationToken>(), false, false, It.IsAny<Func<string, Task>>(), null))
        .ReturnsAsync((partialResponse, 500))
        .Callback(() => cancellationTokenSource.Cancel());

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, partialResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationTokenSource.Token);

    // Assert
    result.Content.Should().Be(partialResponse);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseAsync(
        It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>()),
        Times.Never);
  }

  /// <summary>
  /// Verifies that when OpenAI returns an error response, it is not cached for future use.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_ErrorResponse_DoesNotCache()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Query that fails";
    var errorResponse = "Sorry, I'm having trouble responding right now. Please try again.";
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 2, isNewsRelated: false);
    var context = CreateChatMessages(1);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync((string?)null);

    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, cancellationToken, false, false, It.IsAny<Func<string, Task>>(), null))
        .ReturnsAsync((errorResponse, 0));

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, errorResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(errorResponse);
    _mockLLMCacheService.Verify(x => x.SetCachedResponseAsync(
        It.IsAny<string>(), It.IsAny<List<ChatMessage>>(), It.IsAny<string>()),
        Times.Never);
  }

  #endregion

  #region ProcessUserMessageAsync - High Token Count Tests

  /// <summary>
  /// Verifies that when token count exceeds 50,000, the chat context status is changed to prevent context overflow.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_HighTokenCount_ChangesContextStatus()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Complex query";
    var aiResponse = "Detailed response";
    var highTokenCount = 55000; // Above 50,000 threshold
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 5, isNewsRelated: false);
    var context = CreateChatMessages(4);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync((string?)null);

    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, cancellationToken, false, false, It.IsAny<Func<string, Task>>(), null))
        .ReturnsAsync((aiResponse, highTokenCount));

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, aiResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(aiResponse);
    _mockChatCacheService.Verify(x => x.ChangeCachedChatContextCountStatus(userId, chatId), Times.Once);
    _mockChatRepo.Verify(x => x.ChangeContextStatus(chatId), Times.Once);
  }

  /// <summary>
  /// Verifies that when token count is below the 50,000 threshold, the context status remains unchanged.
  /// </summary>
  [Fact]
  public async Task ProcessUserMessageAsync_NormalTokenCount_DoesNotChangeContextStatus()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var userId = "user123";
    var userMessage = "Simple query";
    var aiResponse = "Simple response";
    var normalTokenCount = 5000; // Below 50,000 threshold
    var cancellationToken = CancellationToken.None;

    var chat = CreateMockChat(chatId, userId, messageCount: 3, isNewsRelated: false);
    var context = CreateChatMessages(2);

    SetupBasicMocks(chatId, userId, chat, context);

    _mockLLMCacheService.Setup(x => x.GetCachedResponseAsync(userMessage, context))
        .ReturnsAsync((string?)null);

    _mockOpenAIService.Setup(x => x.GenerateResponseAsync(
            userMessage, context, cancellationToken, false, false, It.IsAny<Func<string, Task>>(), null))
        .ReturnsAsync((aiResponse, normalTokenCount));

    var userMessageEntity = CreateMockChatMessage(chatId, userMessage, MessageType.User);
    var aiMessageEntity = CreateMockChatMessage(chatId, aiResponse, MessageType.Assistant);

    _mockMessagesRepo.SetupSequence(x => x.AddMessageAsync(It.IsAny<ChatMessage>()))
        .ReturnsAsync(userMessageEntity)
        .ReturnsAsync(aiMessageEntity);

    SetupMapperForMessages(userMessageEntity, aiMessageEntity);

    // Act
    var result = await _chatService.ProcessUserMessageAsync(
        chatId, userMessage, userId, cancellationToken);

    // Assert
    result.Content.Should().Be(aiResponse);
    _mockChatCacheService.Verify(x => x.ChangeCachedChatContextCountStatus(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    _mockChatRepo.Verify(x => x.ChangeContextStatus(It.IsAny<Guid>()), Times.Never);
  }

  #endregion

  #region Helper Methods

  private ChatDomain CreateMockChat(Guid chatId, string userId, int messageCount, bool isNewsRelated, string? newsUrl = null, string? newsContent = null)
  {
    return new ChatDomain
    {
      Id = chatId,
      UserId = userId,
      Title = "Test Chat",
      CreatedAt = DateTime.UtcNow,
      LastMessageAt = DateTime.UtcNow,
      MessageCount = messageCount,
      isChatRelatedToNewsSource = isNewsRelated,
      relatedNewsSourceURL = newsUrl,
      relatedNewsSourceContent = newsContent
    };
  }

  private List<ChatMessage> CreateChatMessages(int count)
  {
    var messages = new List<ChatMessage>();
    for (int i = 0; i < count; i++)
    {
      messages.Add(new ChatMessage
      {
        Id = Guid.NewGuid(),
        ChatId = Guid.NewGuid(),
        Content = $"Message {i}",
        Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
        CreatedAt = DateTime.UtcNow.AddMinutes(-i)
      });
    }
    return messages;
  }

  private ChatMessage CreateMockChatMessage(Guid chatId, string content, MessageType type)
  {
    return new ChatMessage
    {
      Id = Guid.NewGuid(),
      ChatId = chatId,
      Content = content,
      Type = type,
      CreatedAt = DateTime.UtcNow
    };
  }

  private void SetupBasicMocks(Guid chatId, string userId, ChatDomain chat, List<ChatMessage> context)
  {
    _mockChatCacheService.Setup(x => x.GetCachedChat(userId, chatId))
        .ReturnsAsync((CachedChatData?)null);

    _mockMessagesRepo.Setup(x => x.GetMessagesAsync(chatId))
        .ReturnsAsync(context);

    _mockChatRepo.Setup(x => x.GetChatByIdAndUserIdAsync(chatId, userId))
        .ReturnsAsync(chat);

    _mockChatRepo.Setup(x => x.UpdateChatMessageCountAndLastMessageAsync(chatId))
        .Returns(Task.CompletedTask);

    _mockChatCacheService.Setup(x => x.AddMessageToCachedChat(userId, chatId, It.IsAny<ChatMessage>()))
        .Returns(Task.CompletedTask);

    _mockChatCacheService.Setup(x => x.ChangeCachedChatContextCountStatus(userId, chatId))
        .Returns(Task.CompletedTask);

    _mockChatRepo.Setup(x => x.ChangeContextStatus(chatId))
        .Returns(Task.CompletedTask);
  }

  private void SetupMapperForMessages(ChatMessage userMessage, ChatMessage aiMessage)
  {
    _mockMapper.Setup(x => x.Map<FullMessageDto>(It.Is<ChatMessage>(m => m.Id == userMessage.Id)))
        .Returns(new FullMessageDto
        {
          Id = userMessage.Id,
          Content = userMessage.Content,
          Type = userMessage.Type,
          CreatedAt = userMessage.CreatedAt
        });

    _mockMapper.Setup(x => x.Map<FullMessageDto>(It.Is<ChatMessage>(m => m.Id == aiMessage.Id)))
        .Returns(new FullMessageDto
        {
          Id = aiMessage.Id,
          Content = aiMessage.Content,
          Type = aiMessage.Type,
          CreatedAt = aiMessage.CreatedAt
        });
  }

  #endregion
}
