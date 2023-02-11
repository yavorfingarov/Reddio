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
            var retries = 0;
            while (retries < 10 * 60)
            {
                Thread.Sleep(1000);
                var request = new HttpRequestMessage(HttpMethod.Head, "/api/health");
                var response = Client.SendAsync(request).Result;
                if ((int)response.StatusCode == 200)
                {
                    break;
                }
                retries++;
            }
        }
    }
}
