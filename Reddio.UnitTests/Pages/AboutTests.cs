using Reddio.Pages;

namespace Reddio.UnitTests.Pages
{
    public class AboutTests : TestBaseFor<AboutModel>
    {
        [Fact]
        public void OnGet_PopulatesModel()
        {
            var lastImport = DateTime.UtcNow.AddHours(-1);
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = lastImport });
            AddStation(1, "TestStation1", 30, 45);
            AddStation(2, "TestStation2", 20, 5);
            AddStation(3, "TestStation3", 10, 20);
            AddStation(4, "TestStation4", 40, 18);
            var aboutModel = new AboutModel(Db);

            aboutModel.OnGet();

            Assert.Equal(new[] { ("TestStation3", 20), ("TestStation2", 5), ("TestStation1", 45), ("TestStation4", 18) },
                aboutModel.Stations);
            Assert.Equal(lastImport, aboutModel.LastUpdate);
        }

        private void AddStation(int stationId, string stationName, int displayOrder, int trackCount)
        {
            Db.Execute("INSERT INTO Station (Id, Name, DisplayOrder) VALUES (@Id, @Name, @DisplayOrder)",
                new { Id = stationId, Name = stationName, DisplayOrder = displayOrder });
            for (var i = 0; i < trackCount; i++)
            {
                Db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                    new { StationId = stationId, ThreadId = $"thread{i}", Title = $"Title{i}", Url = $"https://reddio.test/track{i}" });
            }
        }
    }
}
