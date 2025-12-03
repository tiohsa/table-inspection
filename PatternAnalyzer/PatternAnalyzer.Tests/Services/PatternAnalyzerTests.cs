using FluentAssertions;
using PatternAnalyzer.Core.Models;
using PatternAnalyzer.Infrastructure.Services;
using Xunit;

namespace PatternAnalyzer.Tests.Services;

public class PatternAnalyzerTests
{
    [Fact]
    public void PrefixStrategy_ShouldFindCommonPrefix()
    {
        // Arrange
        var strategy = new PrefixAnalysisStrategy();
        var data = new List<object> { "ABC001", "ABC002", "ABC999" };
        var request = new AnalysisRequest { Column = "TestCol", AnalysisType = AnalysisType.Prefix };

        // Act
        var result = strategy.Execute(data, request);

        // Assert
        result.Success.Should().BeTrue();
        result.CommonPrefix.Should().Be("ABC");
        result.MatchCount.Should().Be(3);
    }

    [Fact]
    public void PrefixStrategy_ShouldHandleNoPrefix()
    {
        // Arrange
        var strategy = new PrefixAnalysisStrategy();
        var data = new List<object> { "ABC", "DEF", "GHI" };
        var request = new AnalysisRequest { Column = "TestCol", AnalysisType = AnalysisType.Prefix };

        // Act
        var result = strategy.Execute(data, request);

        // Assert
        result.Success.Should().BeTrue();
        result.CommonPrefix.Should().BeEmpty();
    }

    [Fact]
    public void SequenceStrategy_ShouldDetectSequence()
    {
        // Arrange
        var strategy = new SequenceAnalysisStrategy();
        var data = new List<object> { 1, 2, 3, 4, 5 };
        var request = new AnalysisRequest { Column = "ID", AnalysisType = AnalysisType.Sequence };

        // Act
        var result = strategy.Execute(data, request);

        // Assert
        result.Success.Should().BeTrue();
        result.IsSequential.Should().BeTrue();
    }

    [Fact]
    public void SequenceStrategy_ShouldDetectGap()
    {
        // Arrange
        var strategy = new SequenceAnalysisStrategy();
        var data = new List<object> { 1, 2, 4, 5 };
        var request = new AnalysisRequest { Column = "ID", AnalysisType = AnalysisType.Sequence };

        // Act
        var result = strategy.Execute(data, request);

        // Assert
        result.Success.Should().BeTrue();
        result.IsSequential.Should().BeFalse();
        result.SampleViolations.Should().Contain(v => v.Contains("2 -> 4"));
    }

    [Fact]
    public void RegexStrategy_ShouldMatchPattern()
    {
        // Arrange
        var strategy = new RegexAnalysisStrategy();
        var data = new List<object> { "user_1", "user_2", "admin" };
        var request = new AnalysisRequest
        {
            Column = "Username",
            AnalysisType = AnalysisType.Regex,
            RegexPattern = "^user_\\d+$"
        };

        // Act
        var result = strategy.Execute(data, request);

        // Assert
        result.Success.Should().BeTrue();
        result.MatchCount.Should().Be(2);
        result.SampleViolations.Should().Contain("admin");
    }
}
