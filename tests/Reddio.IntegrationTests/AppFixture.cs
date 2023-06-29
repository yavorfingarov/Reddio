using Dapper;
using DbUp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Reddio.IntegrationTests
{
    public sealed class AppFixture
    {
        public HttpClient Client { get; }

        public HttpClient ExternalClient { get; }

        public AppFixture()
        {
            SeedData();
            var factory = new WebApplicationFactory<Program>();
            Client = factory.CreateClient();
            ExternalClient = new HttpClient();
        }

        private static void SeedData()
        {
            Directory.CreateDirectory("bin/Debug/data");
            var connectionString = "DataSource=bin/Debug/data/Reddio.db;Mode=ReadWriteCreate;Cache=Shared";
            var upgrader = DeployChanges.To
                .SQLiteDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
                .LogToNowhere()
                .Build();
            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                throw result.Error;
            }
            var db = new SqliteConnection(connectionString);
            if (db.QueryFirstOrDefault<int?>("SELECT Id FROM Track") != null)
            {
                return;
            }
            for (var i = 0; i < 250; i++)
            {
                db.Execute("INSERT INTO Track (StationId, ThreadId, Title, Url) VALUES (@StationId, @ThreadId, @Title, @Url)",
                    new { StationId = 1, ThreadId = $"thread{i}", Title = $"Title{i}", Url = $"https://reddio.test/track{i}" });
            }
        }
    }
}
