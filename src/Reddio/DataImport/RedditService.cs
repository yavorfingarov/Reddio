using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Reddio.DataImport
{
    public interface IRedditService
    {
        Task<IEnumerable<CommentThreadData>> GetListingAsync(string subreddit,
            int threadCount, string sort, string? period, CancellationToken cancellationToken);
    }

    public class RedditService : IRedditService
    {
        private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        private readonly HttpClient _HttpClient;

        private readonly RedditConfiguration _RedditConfiguration;

        private readonly IMemoryCache _MemoryCache;

        public RedditService(HttpClient httpClient, RedditConfiguration redditConfiguration, IMemoryCache memoryCache)
        {
            _HttpClient = httpClient;
            _RedditConfiguration = redditConfiguration;
            _MemoryCache = memoryCache;
            _HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent",
                string.Format(CultureInfo.InvariantCulture, _RedditConfiguration.UserAgent, _RedditConfiguration.Username));
        }

        public async Task<IEnumerable<CommentThreadData>> GetListingAsync(string subreddit,
            int threadCount, string sort, string? period, CancellationToken cancellationToken)
        {
            var listing = new List<CommentThreadData>();
            var batchSizes = Enumerable.Repeat(_RedditConfiguration.BatchSize, threadCount / _RedditConfiguration.BatchSize)
                .Concat(threadCount % _RedditConfiguration.BatchSize == 0 ?
                    Enumerable.Empty<int>() : new[] { threadCount % _RedditConfiguration.BatchSize })
                .ToArray();
            string? after = null;
            foreach (var currentBatchSize in batchSizes)
            {
                var url = $"https://oauth.reddit.com/r/{subreddit}/{sort}?limit={currentBatchSize}";
                if (period != null)
                {
                    url += $"&t={period}";
                }
                if (after != null)
                {
                    url += $"&after={after}";
                }
                var listingResponse = await GetListingAsync(url, cancellationToken);
                listing.AddRange(listingResponse.Data.Children.Select(c => c.Data));
                if (listingResponse.Data.After == null)
                {
                    break;
                }
                after = listingResponse.Data.After;
            }

            return listing;
        }

        private async Task<ListingResponse> GetListingAsync(string url, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var authorizationHeader = await _MemoryCache.GetOrCreateAsync("OAuthToken", async entry =>
            {
                var token = await GetTokenAsync(cancellationToken);
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn - 5);

                return $"{token.TokenType} {token.AccessToken}";
            });
            request.Headers.Add("authorization", authorizationHeader);
            var response = await _HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var rateLimitRemaining = (int)double.Parse(response.Headers.GetValues("x-ratelimit-remaining").Single(), CultureInfo.InvariantCulture);
            if (rateLimitRemaining == 0)
            {
                var rateLimitReset = (int)double.Parse(response.Headers.GetValues("x-ratelimit-reset").Single(), CultureInfo.InvariantCulture);
                await Task.Delay((rateLimitReset * 1000) + 2000, cancellationToken);
            }
            var listingResponse = await response.Content.ReadFromJsonAsync<ListingResponse>(_JsonSerializerOptions, cancellationToken);
            if (listingResponse?.Data?.Children == null || !listingResponse.Data.Children.Any())
            {
                throw new InvalidOperationException("Failed to get listing response.");
            }

            return listingResponse;
        }

        private async Task<TokenResponse> GetTokenAsync(CancellationToken cancellationToken)
        {
            var url = $"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={_RedditConfiguration.Username}&password={_RedditConfiguration.Password}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{_RedditConfiguration.ClientId}:{_RedditConfiguration.ClientSecret}"));
            request.Headers.Add("authorization", $"basic {basicToken}");
            var response = await _HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_JsonSerializerOptions, cancellationToken);
            if (token == null || string.IsNullOrWhiteSpace(token.TokenType) ||
                string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresIn <= 0)
            {
                throw new InvalidOperationException("Failed to get authorization token.");
            }

            return token;
        }
    }

    public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

    public record ListingResponse(ListingData Data);

    public record ListingData(IEnumerable<CommentThread> Children, string? After);

    public record CommentThread(CommentThreadData Data);

    public record CommentThreadData(string Id, string Title, string Url);

    [Configuration("Reddit")]
    public class RedditConfiguration
    {
        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string ClientId { get; set; } = null!;

        public string ClientSecret { get; set; } = null!;

        public int BatchSize { get; set; }

        public string UserAgent { get; set; } = null!;
    }
}
