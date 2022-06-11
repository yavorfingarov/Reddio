using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class ExceptionHandlerTests : TestBaseFor<ExceptionHandler>
    {
        private readonly Mock<HttpContext> _HttpContextMock;

        public ExceptionHandlerTests()
        {
            _HttpContextMock = new Mock<HttpContext>(MockBehavior.Strict);
        }

        [Fact]
        public async Task Invoke_DoesNothing_WhenNextDoesNotThrow()
        {
            RequestDelegate next = (context) => Task.CompletedTask;
            var exceptionHandler = new ExceptionHandler(next, LoggerMock.Object);

            await exceptionHandler.Invoke(_HttpContextMock.Object);
        }

        [Fact]
        public async Task Invoke_ChangesStatusCodeTo400_WhenBadRequestExceptionIsThrown()
        {
            _HttpContextMock.SetupProperty(c => c.Response.StatusCode, 200);
            RequestDelegate next = (context) => throw new BadHttpRequestException("test");
            var exceptionHandler = new ExceptionHandler(next, LoggerMock.Object);

            await exceptionHandler.Invoke(_HttpContextMock.Object);

            Assert.Equal(400, _HttpContextMock.Object.Response.StatusCode);
        }

        [Fact]
        public async Task Invoke_ChangesStatusCodeTo500_WhenExceptionIsThrown()
        {
            LoggerMock.Setup(LogLevel.Error);
            _HttpContextMock.SetupProperty(c => c.Response.StatusCode, 200);
            var exception = new Exception("test");
            RequestDelegate next = (context) => throw exception;
            var exceptionHandler = new ExceptionHandler(next, LoggerMock.Object);

            await exceptionHandler.Invoke(_HttpContextMock.Object);

            Assert.Equal(500, _HttpContextMock.Object.Response.StatusCode);
            LoggerMock.Verify(LogLevel.Error, "An unhandled exception has occurred", exception);
            LoggerMock.VerifyNoOtherCalls();
        }
    }
}
