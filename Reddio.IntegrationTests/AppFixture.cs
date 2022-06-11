using Microsoft.AspNetCore.Mvc.Testing;

namespace Reddio.IntegrationTests
{
    public class AppFixture
    {
        public HttpClient Client { get; }

        public HttpClient ExternalClient { get; }

        public AppFixture()
        {
            var factory = new WebApplicationFactory<Program>();
            Client = factory.CreateClient();
            ExternalClient = new HttpClient();
        }
    }
}
