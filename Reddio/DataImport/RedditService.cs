using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Reddio.DataImport
{
    public interface IRedditService
    {
        Task<IEnumerable<CommentThreadData>> GetListingAsync(string subreddit, int threadCount, string sort, string? period = null);
    }

    public class RedditService : IRedditService
    {
        private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
        };

        private readonly HttpClient _HttpClient;

        private readonly IConfiguration _Configuration;

        private readonly IMemoryCache _MemoryCache;

        public RedditService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _HttpClient = httpClient;
            _Configuration = configuration;
            _MemoryCache = memoryCache;
            _HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("user-agent",
                string.Format(_Configuration["Reddit:UserAgent"], _Configuration["Reddit:Username"]));
        }

        public async Task<IEnumerable<CommentThreadData>> GetListingAsync(string subreddit,
            int threadCount, string sort, string? period = null)
        {
            var listing = new List<CommentThreadData>();
            var batchSize = int.Parse(_Configuration["Reddit:BatchSize"]);
            var batchSizes = Enumerable.Repeat(batchSize, threadCount / batchSize)
                .Concat(threadCount % batchSize == 0 ? Array.Empty<int>() : new[] { threadCount % batchSize })
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
                var listingResponse = await GetListingAsync(url);
                listing.AddRange(listingResponse.Data.Children.Select(c => c.Data));
                if (listingResponse.Data.After == null)
                {
                    break;
                }
                after = listingResponse.Data.After;
            }

            return listing;
        }

        private async Task<ListingResponse> GetListingAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var authorizationHeader = await _MemoryCache.GetOrCreateAsync("OAuthToken", async entry =>
            {
                var token = await GetTokenAsync();
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

                return $"{token.TokenType} {token.AccessToken}";
            });
            request.Headers.Add("authorization", authorizationHeader);
            var response = await _HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var rateLimitRemaining = (int)double.Parse(response.Headers.GetValues("x-ratelimit-remaining").Single());
            if (rateLimitRemaining == 0)
            {
                await Task.Delay(((int)double.Parse(response.Headers.GetValues("x-ratelimit-reset").Single()) * 1000) + 2000);
            }
            var listingResponse = await response.Content.ReadFromJsonAsync<ListingResponse>(_JsonSerializerOptions);
            if (listingResponse == null || !listingResponse.Data.Children.Any())
            {
                throw new InvalidOperationException("Failed to get listing response.");
            }

            return listingResponse;
        }

        private async Task<TokenResponse> GetTokenAsync()
        {
            var url = $"https://www.reddit.com/api/v1/access_token?grant_type=password" +
                $"&username={GetConfigurationValue("Reddit:Username")}&password={GetConfigurationValue("Reddit:Password")}";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{GetConfigurationValue("Reddit:ClientId")}:{GetConfigurationValue("Reddit:ClientSecret")}"));
            request.Headers.Add("authorization", $"basic {basicToken}");
            var response = await _HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(_JsonSerializerOptions);
            if (token == null || string.IsNullOrWhiteSpace(token.TokenType) || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException("Failed to get authorization token.");
            }

            return token;
        }

        // TODO Remove this once the CI pipeline is stable
        private string GetConfigurationValue(string key)
        {
            var value = _Configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{key} cannot be empty.");
            }

            return value;
        }
    }

    public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

    public record ListingResponse(ListingData Data);

    public record ListingData(IEnumerable<CommentThread> Children, string? After);

    public record CommentThread(CommentThreadData Data);

    public record CommentThreadData(string Id, string Title, string Url);
}
