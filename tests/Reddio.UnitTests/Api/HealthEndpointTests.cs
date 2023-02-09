using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class HealthEndpointTests : TestBaseFor<HealthEndpoint>
    {
        [Fact]
        public void Handle_Returns500_WhenLastImportIsInitial()
        {
            var result = HealthEndpoint.Handle(Db, DataImportConfiguration);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns500_WhenLastImportIsOlder()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-14) });

            var result = HealthEndpoint.Handle(Db, DataImportConfiguration);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns200_WhenLastImportIsFresh()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-13) });

            var result = HealthEndpoint.Handle(Db, DataImportConfiguration);

            Assert.Equal(200, result.GetStatusCode());
        }
    }
}
