using Reddio.Pages;

namespace Reddio.UnitTests.Pages
{
    public class IndexTests : TestBaseFor<IndexModel>
    {
        [Fact]
        public void OnGet_PopulatesModel()
        {
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation1", DisplayOrder = 20 });
            Db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                new { StationId = 1, ThreadId = $"thread1", Title = $"Title1", Url = $"https://reddio.test/track1" });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation2", DisplayOrder = 40 });
            Db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                new { StationId = 2, ThreadId = $"thread2", Title = $"Title2", Url = $"https://reddio.test/track2" });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation4", DisplayOrder = 30 });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation3", DisplayOrder = 10 });
            Db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                new { StationId = 4, ThreadId = $"thread3", Title = $"Title3", Url = $"https://reddio.test/track3" });
            var indexModel = new IndexModel(Db);

            indexModel.OnGet();

            Assert.Equal(new[] { "TestStation3", "TestStation1", "TestStation2" }, indexModel.Stations);
        }
    }
}
