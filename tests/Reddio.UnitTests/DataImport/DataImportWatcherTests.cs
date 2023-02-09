using Microsoft.AspNetCore.Http;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class DataImportWatcherTests
    {
        private readonly DataImportWatcher _DataImportWatcher;

        private readonly Mock<HttpContext> _HttpContextMock;

        private readonly RequestDelegate _Next;

        private bool _IsNextInvoked;

        public DataImportWatcherTests()
        {
            _DataImportWatcher = new DataImportWatcher();
            _HttpContextMock = new Mock<HttpContext>(MockBehavior.Strict);
            _Next = (context) =>
            {
                _IsNextInvoked = true;

                return Task.CompletedTask;
            };
            _IsNextInvoked = false;
        }

        [Fact]
        public void IsPerformingFreshImport_IsFalse_ByDefault()
        {
            Assert.False(_DataImportWatcher.IsPerformingFreshImport);
        }

        [Fact]
        public async Task InvokeAsync_DoesNothing_WhenIsNotPerformingFreshImport()
        {
            _HttpContextMock.SetupProperty(c => c.Response.StatusCode, 200);
            _DataImportWatcher.IsPerformingFreshImport = false;

            await _DataImportWatcher.InvokeAsync(_HttpContextMock.Object, _Next);

            Assert.True(_IsNextInvoked);
            Assert.Equal(200, _HttpContextMock.Object.Response.StatusCode);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(404)]
        [InlineData(500)]
        [InlineData(503)]
        public async Task InvokeAsync_DoesNothing_WhenIsPerformingFreshImportAndResponseStatusCodeIsNotOk(int statusCode)
        {
            _HttpContextMock.SetupProperty(c => c.Response.StatusCode, statusCode);
            _DataImportWatcher.IsPerformingFreshImport = true;

            await _DataImportWatcher.InvokeAsync(_HttpContextMock.Object, _Next);

            Assert.True(_IsNextInvoked);
            Assert.Equal(statusCode, _HttpContextMock.Object.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ChangesStatusCodeTo503_WhenIsPerformingFreshImportAndResponseStatusCodeIsOk()
        {
            _HttpContextMock.SetupProperty(c => c.Response.StatusCode, 200);
            _DataImportWatcher.IsPerformingFreshImport = true;

            await _DataImportWatcher.InvokeAsync(_HttpContextMock.Object, _Next);

            Assert.False(_IsNextInvoked);
            Assert.Equal(503, _HttpContextMock.Object.Response.StatusCode);
        }
    }
}
