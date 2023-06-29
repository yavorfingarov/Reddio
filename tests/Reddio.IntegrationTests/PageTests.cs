using System.Net.Http.Json;
using HtmlAgilityPack;
using Reddio.Api;

namespace Reddio.IntegrationTests
{
    public class PageTests : IClassFixture<AppFixture>
    {
        private readonly AppFixture _Fixture;

        private readonly HashSet<string> _VisitedPaths = new();

        public PageTests(AppFixture fixture)
        {
            _Fixture = fixture;
        }

        [Fact]
        public async Task Get_Index_ReturnsPage()
        {
            await Check("/", async htmlRoot =>
            {
                var staionNames = htmlRoot.SelectNodes("//main/ul/li/a").Select(l => l.InnerText);
                Assert.NotEmpty(staionNames);
                foreach (var stationName in staionNames)
                {
                    var body = JsonContent.Create(new QueueRequest(stationName, Enumerable.Empty<string>()));
                    var response = await _Fixture.Client.PostAsync("/api/queue", body);
                    response.EnsureSuccessStatusCode();
                    var tracks = await response.Content.ReadFromJsonAsync<IEnumerable<Track>>();
                    Assert.NotNull(tracks);
                    Assert.NotEmpty(tracks);
                }
            });
        }

        [Fact]
        public async Task Get_Listen_ReturnsPage()
        {
            await Check("/r/Jazz", htmlRoot =>
            {
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/div"));
                Assert.NotEmpty(htmlRoot.SelectNodes("//main//a"));
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/script"));
            });
        }

        [Fact]
        public async Task Get_History_ReturnsPage()
        {
            await Check("/History", htmlRoot =>
            {
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/div"));
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/script"));
            });
        }

        [Fact]
        public async Task Get_About_ReturnsPage()
        {
            await Check("/About", htmlRoot =>
            {
                Assert.Equal("About", htmlRoot.SelectSingleNode("//main/h1").InnerText);
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/p"));
                Assert.Equal("Metadata", htmlRoot.SelectSingleNode("//main/h2").InnerText);
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/table/tr/th"));
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/table/tr/td"));
            });
        }

        [Fact]
        public async Task Get_Privacy_ReturnsPage()
        {
            await Check("/Privacy", htmlRoot =>
            {
                Assert.Equal("Privacy", htmlRoot.SelectSingleNode("//main/h1").InnerText);
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/p"));
                Assert.NotEmpty(htmlRoot.SelectNodes("//main//a"));
            });
        }

        [Fact]
        public async Task Get_License_ReturnsPage()
        {
            await Check("/License", htmlRoot =>
            {
                Assert.Equal("MIT License", htmlRoot.SelectSingleNode("//main/h1").InnerText);
                Assert.NotEmpty(htmlRoot.SelectNodes("//main/p"));
            });
        }

        [Theory]
        [InlineData("/foo/bar")]
        [InlineData("/r/InvalidStation")]
        [InlineData("/Error")]
        public async Task Get_InvalidPath_ReturnsErrorPage(string path)
        {
            await Check(path, htmlRoot =>
            {
                Assert.Equal("404", htmlRoot.SelectSingleNode("//main/h1").InnerText);
                Assert.Equal("Not Found", htmlRoot.SelectNodes("//main/p")[0].InnerText);
                Assert.Contains("Request ID", htmlRoot.SelectNodes("//main/p")[1].InnerText);
            }, expectedStatusCode: 404);
        }

        [Theory]
        [InlineData("/icon32.png")]
        [InlineData("/icon192.png")]
        [InlineData("/manifest.json")]
        [InlineData("/styles.css")]
        [InlineData("/shared.js")]
        [InlineData("/cookie-policy.js")]
        [InlineData("/player.js")]
        [InlineData("/history.js")]
        public async Task Get_StaticFile_ReturnsFile(string path)
        {
            var response = await _Fixture.Client.GetAsync(path);

            Assert.Equal(200, (int)response.StatusCode);
            Assert.True(response.Content.Headers.ContentLength > 0);
        }

        private async Task Check(string path, Action<HtmlNode> action, int expectedStatusCode = 200)
        {
            var response = await _Fixture.Client.GetAsync(path);

            Assert.Equal(expectedStatusCode, (int)response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", response.Content?.Headers?.ContentType?.ToString());

            var document = new HtmlDocument();
            var bodyStream = await response.Content?.ReadAsStreamAsync()!;
            document.Load(bodyStream);
            var htmlRoot = document.DocumentNode;

            CheckLayout(htmlRoot);

            await CheckLinks(htmlRoot);

            action(htmlRoot);
        }

        private async Task CheckLinks(HtmlNode htmlRoot)
        {
            var links = htmlRoot.SelectNodes("//a")
                .Where(a => a.Attributes.Contains("href"))
                .Select(a => a.Attributes["href"].Value);
            foreach (var link in links)
            {
                if (_VisitedPaths.Contains(link) || link.StartsWith("https://", StringComparison.InvariantCulture))
                {
                    continue;
                }

                var response = await _Fixture.Client.GetAsync(link);

                Assert.Equal(200, (int)response.StatusCode);
                Assert.Contains("text/html", response.Content?.Headers?.ContentType?.ToString());

                _VisitedPaths.Add(link);
            }
        }

        private static void CheckLayout(HtmlNode htmlRoot)
        {
            Assert.Equal("en", htmlRoot.SelectSingleNode("/html").Attributes["lang"].Value);

            Assert.Equal(new[] { "head", "body" }, htmlRoot.SelectNodes("/html/*").Select(n => n.Name));

            Assert.Equal(new[] { "meta", "meta", "meta", "meta", "meta", "meta", "title", "link", "link", "link" },
                htmlRoot.SelectNodes("/html/head/*").Select(n => n.Name));

            var metaNodes = htmlRoot.SelectNodes("/html/head/meta");
            Assert.Equal(6, metaNodes.Count);
            Assert.Equal("utf-8", metaNodes[0].Attributes["charset"].Value);
            Assert.Equal("viewport", metaNodes[1].Attributes["name"].Value);
            Assert.Equal("width=device-width, initial-scale=1", metaNodes[1].Attributes["content"].Value);
            Assert.Equal("description", metaNodes[2].Attributes["name"].Value);
            Assert.Equal("Listen to an exquisite music selection from Reddit's most cultured communities.",
                metaNodes[2].Attributes["content"].Value);
            Assert.Equal("keywords", metaNodes[3].Attributes["name"].Value);
            Assert.Equal("radio, reddit, music", metaNodes[3].Attributes["content"].Value);
            Assert.Equal("author", metaNodes[4].Attributes["name"].Value);
            Assert.Equal("Yavor Fingarov", metaNodes[4].Attributes["content"].Value);
            Assert.Equal("theme-color", metaNodes[5].Attributes["name"].Value);
            Assert.Equal("#a92e00", metaNodes[5].Attributes["content"].Value);

            Assert.Contains("reddio", htmlRoot.SelectSingleNode("/html/head/title").InnerText);

            Assert.Equal(new[] { "stylesheet", "manifest", "icon" },
                htmlRoot.SelectNodes("/html/head/link").Select(n => n.Attributes["rel"].Value));
            Assert.Equal(new[] { "/styles.css", "/manifest.json", "/icon32.png" },
                htmlRoot.SelectNodes("/html/head/link").Select(n => n.Attributes["href"].Value));

            Assert.Equal(new[] { "header", "main", "script", "noscript", "footer" },
                htmlRoot.SelectNodes("/html/body/*").Select(n => n.Name));

            Assert.Equal("reddio", htmlRoot.SelectSingleNode("/html/body/header/div").InnerText.Trim());

            Assert.Equal(2, htmlRoot.SelectNodes("/html/body/footer/*").Count);
            AssertLinks(htmlRoot.SelectNodes("/html/body/footer/div[1]/*"),
                ("/About", "About"), ("/Privacy", "Privacy"), ("/License", "License"),
                ("https://github.com/yavorfingarov/Reddio", "GitHub"));
            Assert.Contains("Build:", htmlRoot.SelectNodes("/html/body/footer/div[2]").Single().InnerText);
        }

        private static void AssertLinks(HtmlNodeCollection nodes, params (string Route, string Text)[] links)
        {
            Assert.Equal(links.Length, nodes.Count);
            Assert.True(nodes.All(n => n.Name == "a"));
            Assert.Equal(links.Select(n => n.Route), nodes.Select(n => n.Attributes["href"].Value));
            Assert.Equal(links.Select(n => n.Text), nodes.Select(n => n.InnerText));
        }
    }
}
