using System.Net.Http.Json;
using Reddio.Api;

namespace Reddio.IntegrationTests
{
    public class ApiTests : IClassFixture<AppFixture>
    {
        private readonly AppFixture _Fixture;

        public ApiTests(AppFixture fixture)
        {
            _Fixture = fixture;
        }

        [Fact]
        public async Task Head_Health_Returns200()
        {
            var request = new HttpRequestMessage(HttpMethod.Head, "/api/health");

            var response = await _Fixture.Client.SendAsync(request);

            Assert.Equal(200, (int)response.StatusCode);
        }

        [Fact]
        public async Task Post_Queue_Returns400_WhenRequestPayloadIsInvalid()
        {
            var response1 = await _Fixture.Client.PostAsJsonAsync("/api/queue", new { Foo = "Bar" });
            var response2 = await _Fixture.Client.PostAsJsonAsync("/api/queue", new { Station = "TestStation" });
            var response3 = await _Fixture.Client.PostAsJsonAsync("/api/queue", new { IgnoreThreadIds = "Test" });

            Assert.Equal(400, (int)response1.StatusCode);
            Assert.Equal(400, (int)response2.StatusCode);
            Assert.Equal(400, (int)response3.StatusCode);
        }

        [Fact]
        public async Task Post_Queue_Returns400_WhenStationDoesNotExist()
        {
            var response = await _Fixture.Client.PostAsJsonAsync("/api/queue", new QueueRequest("TestStation", Enumerable.Empty<string>()));

            Assert.Equal(400, (int)response.StatusCode);
        }

        [Fact]
        public async Task Post_Queue_ReturnsQueue_WhenIgnoreThreadIdsIsEmpty()
        {
            var response = await _Fixture.Client.PostAsJsonAsync("/api/queue", new QueueRequest("Jazz", Enumerable.Empty<string>()));
            var tracks = await response.Content.ReadFromJsonAsync<IEnumerable<Track>>();

            Assert.True(tracks?.Any());
            Assert.True(tracks?.All(t => !string.IsNullOrWhiteSpace(t.ThreadId)));
            Assert.True(tracks?.All(t => !string.IsNullOrWhiteSpace(t.Title)));
            Assert.True(tracks?.All(t => !string.IsNullOrWhiteSpace(t.Url)));
        }

        [Fact]
        public async Task Post_Queue_ReturnsQueue_WhenIgnoreThreadIdsIsNotEmpty()
        {
            var response1 = await _Fixture.Client.PostAsJsonAsync("/api/queue", new QueueRequest("Jazz", Enumerable.Empty<string>()));

            Assert.Equal(200, (int)response1.StatusCode);

            var tracks1 = await response1.Content.ReadFromJsonAsync<IEnumerable<Track>>();
            var threadIds1 = tracks1?.Select(t => t.ThreadId);

            Assert.True(threadIds1?.Any());

            var ignoreThreadIds = threadIds1?.Take(5);
            var response2 = await _Fixture.Client.PostAsJsonAsync("/api/queue", new QueueRequest("Jazz", ignoreThreadIds!));

            Assert.Equal(200, (int)response1.StatusCode);

            var tracks2 = await response2.Content.ReadFromJsonAsync<IEnumerable<Track>>();
            var threadIds2 = tracks2?.Select(t => t.ThreadId);

            Assert.True(threadIds2?.Any());
            Assert.True(threadIds2?.ToHashSet().IsProperSupersetOf(threadIds1?.Skip(5)!));
            Assert.DoesNotContain(threadIds2, t => ignoreThreadIds!.Contains(t));
        }

        [Fact]
        public async Task Post_Queue_ReturnsQueue_WhenIgnoreThreadIdsContainsAllAvailableThreadIds()
        {
            var ignoreThreadIds = new HashSet<string>();
            var queueThreadIds = Enumerable.Empty<string>();
            var requestCount = 0;
            while (!ignoreThreadIds.IsProperSupersetOf(queueThreadIds!))
            {
                queueThreadIds!.ToList().ForEach(t => ignoreThreadIds.Add(t));
                var response = await _Fixture.Client.PostAsJsonAsync("/api/queue", new QueueRequest("Jazz", ignoreThreadIds));
                var tracks = await response.Content.ReadFromJsonAsync<IEnumerable<Track>>();
                queueThreadIds = tracks?.Select(t => t.ThreadId);
                requestCount++;
            }

            Assert.True(requestCount > 0);
        }
    }
}
