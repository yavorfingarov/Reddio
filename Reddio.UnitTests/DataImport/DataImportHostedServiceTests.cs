using Microsoft.Extensions.Logging;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class DataImportHostedServiceTests : TestBaseFor<DataImportHostedService>
    {
        private readonly DataImportHostedService _DataImportHostedService;

        private readonly Mock<IDataImportHandler> _DataImportHandlerMock;

        public DataImportHostedServiceTests()
        {
            _DataImportHandlerMock = new Mock<IDataImportHandler>(MockBehavior.Strict);
            _DataImportHostedService = new DataImportHostedService(LoggerMock.Object,
                DataImportConfiguration, Mock.Of<IServiceProvider>(MockBehavior.Strict));
        }

        [Fact]
        public async Task ImportDataAsync_LogsException_WhenDataImportHandlerThrows()
        {
            LoggerMock.Setup(LogLevel.Error);
            var exception = new Exception("test");
            _DataImportHandlerMock.Setup(h => h.HandleAsync()).Throws(exception);

            await _DataImportHostedService.ImportDataAsync(_DataImportHandlerMock.Object);

            _DataImportHandlerMock.Verify(h => h.HandleAsync(), Times.Once);
            LoggerMock.Verify(LogLevel.Error, "Could not import data.", exception);
            LoggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ImportDataAsync_CallsDataImportHandler()
        {
            _DataImportHandlerMock.Setup(h => h.HandleAsync()).Returns(Task.CompletedTask);

            await _DataImportHostedService.ImportDataAsync(_DataImportHandlerMock.Object);

            _DataImportHandlerMock.Verify(h => h.HandleAsync(), Times.Once);
        }
    }
}
