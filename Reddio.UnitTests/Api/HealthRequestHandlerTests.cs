using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class HealthRequestHandlerTests : TestBaseFor<HealthRequestHandler>
    {
        public HealthRequestHandlerTests()
        {
            ConfigurationMock.Setup(c => c["DataImportPeriod"]).Returns("6");
        }

        [Fact]
        public void Handle_Returns500_WhenLastImportIsInitial()
        {
            var result = HealthRequestHandler.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns500_WhenLastImportIsOlder()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-20) });

            var result = HealthRequestHandler.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns200_WhenLastImportIsFresh()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-1) });

            var result = HealthRequestHandler.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(200, result.GetStatusCode());
        }
    }
}
