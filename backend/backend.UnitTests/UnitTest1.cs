using Xunit;
using System;

namespace backend.Tests;

public class DateTimeUtilityTests
{
    [Fact]
    public void ConvertIso8601Duration_ValidDuration_ReturnsCorrectTimeSpan()
    {
        // Arrange - ISO 8601 duration format: PT2M30S = 2 minutes 30 seconds
        string isoDuration = "PT2M30S";

        // Act
        var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);

        // Assert
        Assert.Equal(150, duration.TotalSeconds); // 2 min * 60 + 30 sec = 150 seconds
    }

    [Fact]
    public void ConvertIso8601Duration_VideoLongerThan2Minutes_ReturnsTrue()
    {
        // Arrange
        string longVideoDuration = "PT5M10S"; // 5 minutes 10 seconds

        // Act
        var duration = System.Xml.XmlConvert.ToTimeSpan(longVideoDuration);
        bool isLongerThan2Minutes = duration.TotalSeconds > 120;

        // Assert
        Assert.True(isLongerThan2Minutes);
    }

    [Fact]
    public void ConvertIso8601Duration_VideoShorterThan2Minutes_ReturnsFalse()
    {
        // Arrange
        string shortVideoDuration = "PT1M30S"; // 1 minute 30 seconds

        // Act
        var duration = System.Xml.XmlConvert.ToTimeSpan(shortVideoDuration);
        bool isLongerThan2Minutes = duration.TotalSeconds > 120;

        // Assert
        Assert.False(isLongerThan2Minutes);
    }

    [Fact]
    public void ConvertIso8601Duration_VideoExactly2Minutes_ReturnsFalse()
    {
        // Arrange
        string exactDuration = "PT2M"; // Exactly 2 minutes

        // Act
        var duration = System.Xml.XmlConvert.ToTimeSpan(exactDuration);
        bool isLongerThan2Minutes = duration.TotalSeconds > 120;

        // Assert
        Assert.False(isLongerThan2Minutes); // Should be false as we check for > 120, not >=
    }
}
