using Reddio.Api;

namespace Reddio.UnitTests.Api
{
    public class HealthEndpointTests : TestBaseFor<HealthEndpoint>
    {
        [Fact]
        public void Handle_Returns200()
        {
            var result = HealthEndpoint.Handle();

            Assert.Equal(200, result.GetStatusCode());
        }
    }
}
