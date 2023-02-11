﻿using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class DataImportBackgroundServiceTests : TestBaseFor<DataImportBackgroundService>
    {
        private readonly DataImportBackgroundService _DataImportBackgroundService;

        private readonly Mock<IDataImportHandler> _DataImportHandlerMock;

        private readonly Mock<IServiceScope> _ServiceScopeMock;

        private readonly Stopwatch _Stopwatch = new();

        public DataImportBackgroundServiceTests()
        {
            _DataImportHandlerMock = new Mock<IDataImportHandler>(MockBehavior.Strict);
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
            var serviceProviderMock = new Mock<IServiceProvider>(MockBehavior.Strict);
            _ServiceScopeMock = new Mock<IServiceScope>(MockBehavior.Strict);
            serviceProviderMock.Setup(s => s.GetService(typeof(IDataImportHandler)))
                .Returns(_DataImportHandlerMock.Object);
            _ServiceScopeMock.Setup(s => s.ServiceProvider)
                .Returns(serviceProviderMock.Object);
            _ServiceScopeMock.Setup(s => s.Dispose());
            serviceScopeFactoryMock.Setup(s => s.CreateScope())
                .Returns(_ServiceScopeMock.Object);
            _DataImportBackgroundService = new DataImportBackgroundService(LoggerMock.Object,
                DataImportConfiguration, serviceScopeFactoryMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_LogsException_WhenDataImportHandlerThrows()
        {
            LoggerMock.Setup(LogLevel.Error);
            var exception = new Exception("test");
            _DataImportHandlerMock.Setup(h => h.HandleAsync(It.IsAny<CancellationToken>()))
                .Throws(exception);
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            _Stopwatch.Start();
            await _DataImportBackgroundService.StartAsync(cancellationTokenSource.Token);
            await _DataImportBackgroundService.ExecuteTask!;
            _Stopwatch.Stop();

            Assert.InRange(_Stopwatch.ElapsedMilliseconds, 240, 300);
            _DataImportHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CancellationToken>()), Times.Once);
            LoggerMock.Verify(LogLevel.Error, "Could not import data.", exception);
            LoggerMock.VerifyNoOtherCalls();
            _ServiceScopeMock.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_CallsDataImportHandler()
        {
            _DataImportHandlerMock.Setup(h => h.HandleAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            _Stopwatch.Start();
            await _DataImportBackgroundService.StartAsync(cancellationTokenSource.Token);
            await _DataImportBackgroundService.ExecuteTask!;
            _Stopwatch.Stop();

            Assert.InRange(_Stopwatch.ElapsedMilliseconds, 240, 300);
            _DataImportHandlerMock.Verify(h => h.HandleAsync(It.IsAny<CancellationToken>()), Times.Once);
            _ServiceScopeMock.Verify(s => s.Dispose(), Times.Once);
        }
    }
}
