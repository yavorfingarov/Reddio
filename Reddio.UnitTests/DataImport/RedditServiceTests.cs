using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Moq.Protected;
using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class RedditServiceTests : TestBaseFor<RedditService>
    {
        private const string _Username = "testUsername";

        private const string _Password = "testPassword";

        private const string _ClientId = "testClientId";

        private const string _ClientSecret = "testClientSecret";

        private const int _BatchSize = 100;

        private const string _UserAgent = "test (https://reddio.test) by /u/{0}";

        private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        private readonly RedditService _RedditService;

        private readonly RedditConfiguration _RedditConfiguration;

        private readonly Mock<HttpMessageHandler> _HttpMessageHandlerMock;

        private readonly List<HttpRequestMessage> _Requests;

        public RedditServiceTests()
        {
            _RedditConfiguration = new RedditConfiguration()
            {
                Username = _Username,
                Password = _Password,
                ClientId = _ClientId,
                ClientSecret = _ClientSecret,
                BatchSize = _BatchSize,
                UserAgent = _UserAgent
            };
            _HttpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var httpClient = new HttpClient(_HttpMessageHandlerMock.Object);
            _RedditService = new RedditService(httpClient, _RedditConfiguration, new MemoryCache(new MemoryCacheOptions()));
            _Requests = new List<HttpRequestMessage>();
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue)]
        [InlineData(HttpStatusCode.MultipleChoices)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetListingAsync_Throws_WhenTokenResponseStatusCodeIsNotSuccess(HttpStatusCode statusCode)
        {
            var tokenResponse = new HttpResponseMessage(statusCode);
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);

            await Assert.ThrowsAsync<HttpRequestException>(async () => await _RedditService.GetListingAsync("test", 100, "new"));
            Assert.Equal("test (https://reddio.test) by /u/testUsername", _Requests.Single().Headers?.UserAgent?.ToString());
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests.Single().Headers?.Authorization?.ToString());
        }

        [Fact]
        public async Task GetListingAsync_Throws_WhenTokenResponseBodyIsMalformed()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { }, options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _RedditService.GetListingAsync("test", 100, "new"));
            Assert.Equal("Failed to get authorization token.", exception.Message);
            Assert.Equal("test (https://reddio.test) by /u/testUsername", _Requests.Single().Headers?.UserAgent?.ToString());
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests.Single().Headers?.Authorization?.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue)]
        [InlineData(HttpStatusCode.MultipleChoices)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetListingAsync_Throws_WhenListingResponseStatusCodeIsNotSuccess(HttpStatusCode statusCode)
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponse = new HttpResponseMessage(statusCode);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100", listingResponse);

            await Assert.ThrowsAsync<HttpRequestException>(async () => await _RedditService.GetListingAsync("test", 100, "new"));
            Assert.Equal(2, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.Equal("bearer testToken123", _Requests[1].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        [Fact]
        public async Task GetListingAsync_Throws_WhenListingIsEmpty()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponseBody = new ListingResponse(new ListingData(Enumerable.Empty<CommentThread>(), null));
            var listingResponse = CreateBatchResponse(0, 0, 10);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100", listingResponse);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _RedditService.GetListingAsync("test", 100, "new"));
            Assert.Equal("Failed to get listing response.", exception.Message);
            Assert.Equal(2, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.Equal("bearer testToken123", _Requests[1].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        [Fact]
        public async Task GetListingAsync_ReturnsListing_WhenSingleBatchIsRequested()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponse = CreateBatchResponse(0, 100, 10);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100", listingResponse);

            var listing = await _RedditService.GetListingAsync("test", 100, "new");
            Assert.Equal(Enumerable.Range(0, 100).Select(i => $"thread{i}"), listing.Select(t => t.Id));
            Assert.Equal(2, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.Equal("bearer testToken123", _Requests[1].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        [Fact]
        public async Task GetListingAsync_ReturnsListing_WhenMultipleBatchesAreRequested()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponse1 = CreateBatchResponse(0, 100, 10);
            var listingResponse2 = CreateBatchResponse(1, 100, 10);
            var listingResponse3 = CreateBatchResponse(2, 50, 10);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100", listingResponse1);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100&after=after0", listingResponse2);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=50&after=after1", listingResponse3);

            var listing = await _RedditService.GetListingAsync("test", 250, "new");
            Assert.Equal(Enumerable.Range(0, 250).Select(i => $"thread{i}").ToList(), listing.Select(t => t.Id).ToList());
            Assert.Equal(4, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Skip(1).Select(r => r.Headers?.Authorization?.ToString())
                .All(h => h == "bearer testToken123"));
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        [Fact]
        public async Task GetListingAsync_ReturnsListing_WhenPeriodIsProvided()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponse = CreateBatchResponse(0, 100, 10);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/best?limit=100&t=day", listingResponse);

            var listing = await _RedditService.GetListingAsync("test", 100, "best", "day");
            Assert.Equal(Enumerable.Range(0, 100).Select(i => $"thread{i}"), listing.Select(t => t.Id));
            Assert.Equal(2, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.Equal("bearer testToken123", _Requests[1].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        [Fact]
        public async Task GetListingAsync_ReturnsListingAndWaits_WhenRateLimitIsReached()
        {
            var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse("testToken123", "bearer", 360), options: _JsonSerializerOptions)
            };
            SetupHttpClientResponse($"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_Username}&password={_Password}", tokenResponse);
            var listingResponse1 = CreateBatchResponse(0, 100, 1);
            var listingResponse2 = CreateBatchResponse(1, 100, 0);
            var listingResponse3 = CreateBatchResponse(2, 50, 10);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100", listingResponse1);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=100&after=after0", listingResponse2);
            SetupHttpClientResponse($"https://oauth.reddit.com/r/test/new?limit=50&after=after1", listingResponse3);

            var sw = Stopwatch.StartNew();
            var listing = await _RedditService.GetListingAsync("test", 250, "new");
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds > 4000);
            Assert.Equal(Enumerable.Range(0, 250).Select(i => $"thread{i}").ToList(), listing.Select(t => t.Id).ToList());
            Assert.Equal(4, _Requests.Count);
            Assert.Equal("basic dGVzdENsaWVudElkOnRlc3RDbGllbnRTZWNyZXQ=", _Requests[0].Headers?.Authorization?.ToString());
            Assert.True(_Requests.Skip(1).Select(r => r.Headers?.Authorization?.ToString())
                .All(h => h == "bearer testToken123"));
            Assert.True(_Requests.Select(r => r.Headers.UserAgent.ToString())
                .All(u => u == "test (https://reddio.test) by /u/testUsername"));
        }

        private void SetupHttpClientResponse(string path, HttpResponseMessage response)
        {
            _HttpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString() == path),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => _Requests.Add(request))
                .Returns(Task.FromResult(response));
        }

        private static HttpResponseMessage CreateBatchResponse(int batchNumber, int thisBatchSize, int rateLimitRemaining)
        {
            var threads = Enumerable.Range(batchNumber * _BatchSize, thisBatchSize)
                .Select(i => new CommentThread(new CommentThreadData($"thread{i}", $"Thread {i}", $"https://reddio.test/thread/{i}")));
            var listingResponseBody = new ListingResponse(new ListingData(threads, $"after{batchNumber}"));
            var listingResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(listingResponseBody, options: _JsonSerializerOptions)
            };
            listingResponse.Headers.Add("x-ratelimit-remaining", rateLimitRemaining.ToString());
            listingResponse.Headers.Add("x-ratelimit-reset", 2.ToString());

            return listingResponse;
        }
    }
}
