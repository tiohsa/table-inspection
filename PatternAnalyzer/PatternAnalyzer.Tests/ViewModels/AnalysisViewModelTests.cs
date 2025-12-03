using FluentAssertions;
using Moq;
using PatternAnalyzer.Core.Interfaces;
using PatternAnalyzer.Core.Models;
using PatternAnalyzer.Presentation.ViewModels;
using Xunit;

namespace PatternAnalyzer.Tests.ViewModels;

public class AnalysisViewModelTests
{
    private readonly Mock<IDatabaseService> _dbServiceMock;
    private readonly Mock<IPatternAnalyzer> _analyzerMock;
    private readonly Mock<IPresetRepository> _presetRepoMock;
    private readonly ConnectionViewModel _connectionVm;

    public AnalysisViewModelTests()
    {
        _dbServiceMock = new Mock<IDatabaseService>();
        _analyzerMock = new Mock<IPatternAnalyzer>();
        _presetRepoMock = new Mock<IPresetRepository>();

        var connVmDbMock = new Mock<IDatabaseService>();
        _connectionVm = new ConnectionViewModel(connVmDbMock.Object);
    }

    [Fact]
    public async Task RunAnalysis_ShouldExecuteAndSetResult()
    {
        // Arrange
        var viewModel = new AnalysisViewModel(_dbServiceMock.Object, _analyzerMock.Object, _presetRepoMock.Object, _connectionVm);
        viewModel.SelectedColumn = new ColumnMetadata { ColumnName = "TestCol" };

        var dummyData = new List<object> { "A", "B" };
        var dummyResult = new AnalysisResult { Success = true, Message = "Done" };

        _dbServiceMock.Setup(x => x.GetDataAsync(It.IsAny<AnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyData);

        _analyzerMock.Setup(x => x.Analyze(dummyData, It.IsAny<AnalysisRequest>()))
            .Returns(dummyResult);

        // Act
        await viewModel.RunAnalysisAsync();

        // Assert
        viewModel.LastResult.Should().NotBeNull();
        viewModel.LastResult!.Success.Should().BeTrue();
        viewModel.StatusMessage.Should().Be("Analysis Complete.");
    }

    [Fact]
    public async Task LoadSchemas_ShouldUpdateCollection()
    {
        // Arrange
        _connectionVm.IsConnected = true; // Simulate connected state
        var schemas = new List<string> { "public", "auth" };
        _dbServiceMock.Setup(x => x.GetSchemasAsync(It.IsAny<ConnectionConfig>()))
            .ReturnsAsync(schemas);

        var viewModel = new AnalysisViewModel(_dbServiceMock.Object, _analyzerMock.Object, _presetRepoMock.Object, _connectionVm);

        // Act
        await viewModel.LoadSchemasAsync();

        // Assert
        viewModel.Schemas.Should().HaveCount(2);
        viewModel.Schemas.Should().Contain("public");
    }
}
