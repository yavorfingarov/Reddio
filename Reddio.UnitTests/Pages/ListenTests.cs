using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Reddio.Pages;

namespace Reddio.UnitTests.Pages
{
    public class ListenTests : TestBaseFor<ListenModel>
    {
        public ListenTests()
        {
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation1", DisplayOrder = 10 });
            Db.Execute("INSERT INTO Station (Name, DisplayOrder) VALUES (@Name, @DisplayOrder)",
                new { Name = "TestStation2", DisplayOrder = 20 });
        }

        [Fact]
        public void OnGet_ReturnsNotFound_WhenStationIsInvalid()
        {
            var listenModel = new ListenModel(Db);

            var result = listenModel.OnGet("InvalidStation");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void OnGet_ReturnsPage_WhenStationIsValid()
        {
            var listenModel = new ListenModel(Db);

            var result = listenModel.OnGet("TestStation2");

            Assert.IsType<PageResult>(result);
        }
    }
}
