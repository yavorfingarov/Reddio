using Microsoft.Extensions.Logging;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class DataImportHandlerTests : TestBaseFor<DataImportHandler>
    {
        private readonly IDataImportHandler _DataImportHandler;

        private readonly Mock<IRedditService> _RedditServiceMock;

        private readonly DataImportWatcher _DataImportWatcher;

        public DataImportHandlerTests()
        {
            ConfigurationMock.Setup(c => c["DataImportPeriod"]).Returns("6");
            _RedditServiceMock = new Mock<IRedditService>(MockBehavior.Strict);
            _DataImportWatcher = new DataImportWatcher();
            _DataImportHandler = new DataImportHandler(Db, _RedditServiceMock.Object, _DataImportWatcher,
                ConfigurationMock.Object, LoggerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_DoesNothing_WhenDataIsFresh()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-1) });

            await _DataImportHandler.HandleAsync();
        }

        [Fact]
        public async Task HandleAsync_ImportsData_WhenDataIsNotFresh()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-8) });
            Db.Execute("INSERT INTO KnownDomain (Domain) VALUES (@Domain)", new { Domain = "known.domain" });
            Db.Execute("INSERT INTO KnownDomain (Domain) VALUES (@Domain)", new { Domain = "2known.domain" });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation1", DisplayOrder = 10 });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation2", DisplayOrder = 20 });
            Db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                new { StationId = 1, ThreadId = "thread1", Title = "Thread Title 1", Url = "https://known.domain/thread1" });
            _RedditServiceMock.Setup(r => r.GetListingAsync("TestStation1", 50, "hot", null))
                .ReturnsAsync(new[]
                {
                    new CommentThreadData("thread1", "Thread Title 1", "https://known.domain/thread1"),
                    new CommentThreadData("thread2", "Thread Title 2", "https://known.domain/thread2"),
                });
            _RedditServiceMock.Setup(r => r.GetListingAsync("TestStation2", 500, "best", "all"))
                .ReturnsAsync(new[]
                {
                    new CommentThreadData("thread3", "Thread Title 3", "https://known.domain/thread3"),
                    new CommentThreadData("thread4", "Thread Title 4", "https://known.domain/thread4"),
                });
            _RedditServiceMock.Setup(r => r.GetListingAsync("TestStation2", 300, "best", "year"))
                .ReturnsAsync(new[]
                {
                    new CommentThreadData("thread5", "Thread Title 5", "http://known.domain/thread5")
                });
            _RedditServiceMock.Setup(r => r.GetListingAsync("TestStation2", 200, "best", "month"))
                .ReturnsAsync(new[]
                {
                    new CommentThreadData("thread6", "Thread Title 6", "https://known.domain/thread6"),
                    new CommentThreadData("thread8", "Thread Title 8", "https://reddio.test/thread8"),
                    new CommentThreadData("thread9", "Thread Title 9", "http://known.domain/thread9"),
                });
            _RedditServiceMock.Setup(r => r.GetListingAsync("TestStation2", 100, "hot", null))
                .ReturnsAsync(new[]
                {
                    new CommentThreadData("thread3", "Thread Title 3", "https://known.domain/thread3"),
                    new CommentThreadData("thread7", "Thread Title 7", "https://2known.domain/thread7")
                });
            LoggerMock.Setup(LogLevel.Debug);

            await _DataImportHandler.HandleAsync();

            Assert.Equal(new[] { "thread1", "thread2" },
                Db.Query<string>("SELECT ThreadId FROM Track WHERE StationId = 1 ORDER BY Id"));
            Assert.Equal(new[] { "thread4", "thread3", "thread5", "thread9", "thread6", "thread7" },
                Db.Query<string>("SELECT ThreadId FROM Track WHERE StationId = 2 ORDER BY Id"));
            Assert.Equal("Thread Title 5",
                Db.QuerySingle<string>("SELECT Title FROM Track WHERE ThreadId = 'thread5'"));
            Assert.Equal("https://known.domain/thread4",
                Db.QuerySingle<string>("SELECT Url FROM Track WHERE ThreadId = 'thread4'"));
            Assert.False(_DataImportWatcher.IsPerformingFreshImport);
            LoggerMock.Verify(LogLevel.Debug, "Importing data...");
            LoggerMock.Verify(LogLevel.Debug, "Data import finished. Rows affected: 7");
        }
    }
}
