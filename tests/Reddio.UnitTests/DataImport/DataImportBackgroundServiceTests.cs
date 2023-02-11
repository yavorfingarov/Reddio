using Microsoft.Extensions.Logging;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class DataImportBackgroundServiceTests : TestBaseFor<DataImportBackgroundService>
    {
        private readonly DataImportBackgroundService _DataImportBackgroundService;

        private readonly Mock<IDataImportHandler> _DataImportHandlerMock;

        public DataImportBackgroundServiceTests()
        {
            _DataImportHandlerMock = new Mock<IDataImportHandler>(MockBehavior.Strict);
            _DataImportBackgroundService = new DataImportBackgroundService(LoggerMock.Object,
                DataImportConfiguration, Mock.Of<IServiceProvider>(MockBehavior.Strict));
        }

        [Fact]
        public async Task ImportDataAsync_LogsException_WhenDataImportHandlerThrows()
        {
            LoggerMock.Setup(LogLevel.Error);
            var exception = new Exception("test");
            _DataImportHandlerMock.Setup(h => h.HandleAsync(CancellationToken.None)).Throws(exception);

            await _DataImportBackgroundService.ImportDataAsync(_DataImportHandlerMock.Object, CancellationToken.None);

            _DataImportHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CancellationToken>()), Times.Once);
            LoggerMock.Verify(LogLevel.Error, "Could not import data.", exception);
            LoggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ImportDataAsync_CallsDataImportHandler()
        {
            _DataImportHandlerMock.Setup(h => h.HandleAsync(CancellationToken.None)).Returns(Task.CompletedTask);

            await _DataImportBackgroundService.ImportDataAsync(_DataImportHandlerMock.Object, CancellationToken.None);

            _DataImportHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
