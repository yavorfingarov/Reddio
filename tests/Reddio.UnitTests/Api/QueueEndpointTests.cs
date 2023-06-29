using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class QueueEndpointTests : TestBaseFor<QueueEndpoint>
    {
        private readonly QueueConfiguration _QueueConfiguration;

        public QueueEndpointTests()
        {
            using var transaction = Db.BeginTransaction();
            for (var i = 1; i <= 2; i++)
            {
                Db.Execute($"INSERT INTO Station (Id, Name, DisplayOrder) VALUES ({i}, 'TestStation{i}', {i})");
            }
            for (var i = 1; i <= 50; i++)
            {
                for (var j = 1; j <= 2; j++)
                {
                    Db.Execute($"INSERT INTO Track (StationId, ThreadId, Title, Url) " +
                        $"VALUES ({j}, 'TestThread{i}', 'Test Title {i}', 'https://test{i}.com')");
                }
            }
            transaction.Commit();
            _QueueConfiguration = new QueueConfiguration()
            {
                Length = 25
            };
        }

        [Fact]
        public void Handle_Returns400_WhenStationIsInvalid()
        {
            var request = new QueueRequest("InvalidStation", Enumerable.Empty<string>());

            var result = QueueEndpoint.Handle(request, Db, _QueueConfiguration);

            Assert.Equal(400, result.GetStatusCode());
            Assert.Null(result.GetValue<IEnumerable<Track>>());
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsIsEmpty()
        {
            var request = new QueueRequest("TestStation1", Enumerable.Empty<string>());

            var result = QueueEndpoint.Handle(request, Db, _QueueConfiguration);

            Assert.Equal(200, result.GetStatusCode());
            var tracks = result.GetValue<IEnumerable<Track>>();
            Assert.Equal(25, tracks?.Count());
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsIsNotEmpty()
        {
            var ignoreThreadIds = Enumerable.Range(1, 30).Select(i => $"TestThread{i}");
            var request = new QueueRequest("TestStation1", ignoreThreadIds);

            var result = QueueEndpoint.Handle(request, Db, _QueueConfiguration);

            Assert.Equal(200, result.GetStatusCode());
            var threadIds = result.GetValue<IEnumerable<Track>>()?
                .Select(t => t.ThreadId)
                .OrderBy(t => t);
            Assert.NotNull(threadIds);
            var expectedThreadIds = Enumerable.Range(31, 20)
                .Select(i => $"TestThread{i}");
            Assert.Equal(expectedThreadIds, threadIds);
        }

        [Fact]
        public void Handle_ReturnsQueue_WhenIgnoreThreadIdsContainsAllAvailableThreadIds()
        {
            var ignoreThreadIds = Enumerable.Range(1, 50).Select(i => $"TestThread{i}");
            var request = new QueueRequest("TestStation1", ignoreThreadIds);

            var result = QueueEndpoint.Handle(request, Db, _QueueConfiguration);

            Assert.Equal(200, result.GetStatusCode());
            var tracks = result.GetValue<IEnumerable<Track>>();
            Assert.Equal(25, tracks?.Count());
        }
    }
}
