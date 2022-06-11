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
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation2", DisplayOrder = 40 });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation4", DisplayOrder = 30 });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation3", DisplayOrder = 10 });
            var indexModel = new IndexModel(Db);

            indexModel.OnGet();

            Assert.Equal(new[] { "TestStation3", "TestStation1", "TestStation4", "TestStation2" }, indexModel.Stations);
        }
    }
}
