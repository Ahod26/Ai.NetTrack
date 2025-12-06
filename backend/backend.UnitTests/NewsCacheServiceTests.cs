using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using backend.Services.Classes.Cache;
using backend.Repository.Interfaces;
using backend.Models.Domain;
using FluentAssertions;

namespace backend.Tests;

public class NewsCacheServiceTests
{
    private readonly Mock<IRedisCacheRepo> _mockNewsCacheRepo;
    private readonly Mock<ILogger<NewsCacheService>> _mockLogger;
    private readonly NewsCacheService _service;

    public NewsCacheServiceTests()
    {
        _mockNewsCacheRepo = new Mock<IRedisCacheRepo>();
        _mockLogger = new Mock<ILogger<NewsCacheService>>();
        _service = new NewsCacheService(_mockNewsCacheRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetNewsAsync_WithValidDates_ReturnsNewsItems()
    {
        // Arrange
        var date1 = new DateTime(2025, 11, 27);
        var date2 = new DateTime(2025, 11, 28);
        var dates = new List<DateTime> { date1, date2 };

        var newsForDate1 = new List<NewsItem>
        {
            new NewsItem
            {
                Id = 1,
                Title = "News 1",
                Url = "https://example.com/news1",
                PublishedDate = date1
            }
        };

        var newsForDate2 = new List<NewsItem>
        {
            new NewsItem
            {
                Id = 2,
                Title = "News 2",
                Url = "https://example.com/news2",
                PublishedDate = date2
            }
        };

        // Setup mock to return different news for different dates
        _mockNewsCacheRepo
            .Setup(x => x.GetNewsAsync("news:date:2025-11-27", 0))
            .ReturnsAsync(newsForDate1);

        _mockNewsCacheRepo
            .Setup(x => x.GetNewsAsync("news:date:2025-11-28", 0))
            .ReturnsAsync(newsForDate2);

        // Act
        var result = await _service.GetNewsAsync(dates, 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Title == "News 1");
        result.Should().Contain(n => n.Title == "News 2");

        // Verify the repository was called twice
        _mockNewsCacheRepo.Verify(x => x.GetNewsAsync(It.IsAny<string>(), 0), Times.Exactly(2));
    }

    [Fact]
    public async Task GetNewsAsync_WithNoNewsInCache_ReturnsEmptyList()
    {
        // Arrange
        var date = new DateTime(2025, 11, 27);
        var dates = new List<DateTime> { date };

        // Setup mock to return null (cache miss)
        _mockNewsCacheRepo
            .Setup(x => x.GetNewsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((List<NewsItem>?)null);

        // Act
        var result = await _service.GetNewsAsync(dates, 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockNewsCacheRepo.Verify(x => x.GetNewsAsync("news:date:2025-11-27", 0), Times.Once);
    }

    [Fact]
    public async Task GetNewsAsync_WhenExceptionThrown_ReturnsEmptyListAndLogsError()
    {
        // Arrange
        var date = new DateTime(2025, 11, 27);
        var dates = new List<DateTime> { date };

        // Setup mock to throw exception
        _mockNewsCacheRepo
            .Setup(x => x.GetNewsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        var result = await _service.GetNewsAsync(dates, 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting news for dates")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNewsAsync_WithMultipleDates_CombinesAllResults()
    {
        // Arrange
        var dates = new List<DateTime>
        {
            new DateTime(2025, 11, 25),
            new DateTime(2025, 11, 26),
            new DateTime(2025, 11, 27)
        };

        // Setup mock to return one item per date
        foreach (var date in dates)
        {
            var dateKey = $"news:date:{date:yyyy-MM-dd}";
            var newsItem = new List<NewsItem>
            {
                new NewsItem
                {
                    Id = date.Day,
                    Title = $"News for {date:yyyy-MM-dd}",
                    Url = $"https://example.com/news{date.Day}",
                    PublishedDate = date
                }
            };

            _mockNewsCacheRepo
                .Setup(x => x.GetNewsAsync(dateKey, 0))
                .ReturnsAsync(newsItem);
        }

        // Act
        var result = await _service.GetNewsAsync(dates, 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().OnlyContain(item => item.Title != null);
        _mockNewsCacheRepo.Verify(x => x.GetNewsAsync(It.IsAny<string>(), 0), Times.Exactly(3));
    }
}
