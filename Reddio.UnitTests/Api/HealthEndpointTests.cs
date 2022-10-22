using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class HealthEndpointTests : TestBaseFor<HealthEndpoint>
    {
        public HealthEndpointTests()
        {
            ConfigurationMock.Setup(c => c["DataImport:Period"]).Returns("6");
            ConfigurationMock.Setup(c => c["DataImport:HostedServicePeriod"]).Returns("1");
        }

        [Fact]
        public void Handle_Returns500_WhenLastImportIsInitial()
        {
            var result = HealthEndpoint.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns500_WhenLastImportIsOlder()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-14) });

            var result = HealthEndpoint.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(500, result.GetStatusCode());
        }

        [Fact]
        public void Handle_Returns200_WhenLastImportIsFresh()
        {
            Db.Execute("UPDATE Metadata SET LastImport = @LastImport", new { LastImport = DateTime.UtcNow.AddHours(-13) });

            var result = HealthEndpoint.Handle(Db, ConfigurationMock.Object);

            Assert.Equal(200, result.GetStatusCode());
        }
    }
}
