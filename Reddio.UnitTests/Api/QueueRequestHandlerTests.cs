using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class QueueRequestHandlerTests : TestBaseFor<QueueRequestHandler>
    {
        public QueueRequestHandlerTests()
        {
            using var transaction = Db.BeginTransaction();
            Enumerable.Range(1, 2).ToList()
                .ForEach(i => Db.Execute($"INSERT INTO Station (Id, Name, DisplayOrder) VALUES ({i}, 'TestStation{i}', {i})"));
            for (var i = 50; i > 0; i--)
            {
                Db.Execute($"INSERT INTO Track (StationId, ThreadId, Title, Url) " +
                    $"VALUES ({((i <= 50) ? 1 : 2)}, 'TestThread{i}', 'Test Title {i}', 'https://test{i}.com')");
            }
            transaction.Commit();
            ConfigurationMock.Setup(c => c["QueueLength"]).Returns("25");
        }

        [Fact]
        public void Handle_Returns400_WhenStationIsInvalid()
        {
            var request = new QueueRequest("InvalidStation", Enumerable.Empty<string>());

            var result = QueueRequestHandler.Handle(request, Db, ConfigurationMock.Object);

            Assert.Equal(400, result.GetStatusCode());
            Assert.Null(result.GetValue<IEnumerable<Track>>());
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsIsEmpty()
        {
            var request = new QueueRequest("TestStation1", Enumerable.Empty<string>());

            var result = QueueRequestHandler.Handle(request, Db, ConfigurationMock.Object);

            Assert.Equal(200, result.GetStatusCode());
            Assert.Equal(Enumerable.Range(1, 25).Select(i => $"TestThread{i}"),
                result.GetValue<IEnumerable<Track>>()?.Select(t => t.ThreadId));
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsIsNotEmpty()
        {
            var request = new QueueRequest("TestStation1", Enumerable.Range(1, 25).Select(i => $"TestThread{i}"));

            var result = QueueRequestHandler.Handle(request, Db, ConfigurationMock.Object);

            Assert.Equal(200, result.GetStatusCode());
            Assert.Equal(Enumerable.Range(26, 25).Select(i => $"TestThread{i}"),
                result.GetValue<IEnumerable<Track>>()?.Select(t => t.ThreadId));
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsContainsAllAvailableThreadIds()
        {
            var request = new QueueRequest("TestStation1", Enumerable.Range(1, 50).Select(i => $"TestThread{i}"));

            var result = QueueRequestHandler.Handle(request, Db, ConfigurationMock.Object);

            Assert.Equal(200, result.GetStatusCode());
            Assert.Equal(Enumerable.Range(1, 25).Select(i => $"TestThread{i}"),
                result.GetValue<IEnumerable<Track>>()?.Select(t => t.ThreadId));
        }
    }
}
