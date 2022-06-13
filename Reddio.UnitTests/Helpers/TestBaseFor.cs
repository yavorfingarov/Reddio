using System.Data;
using DbUp;
using DbUp.SQLite.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Reddio.UnitTests.Helpers
{
    public abstract class TestBaseFor<T> : IDisposable
    {
        public Mock<ILogger<T>> LoggerMock;

        public Mock<IConfiguration> ConfigurationMock;

        public IDbConnection Db;

        public TestBaseFor()
        {
            LoggerMock = new Mock<ILogger<T>>(MockBehavior.Strict);
            ConfigurationMock = new Mock<IConfiguration>(MockBehavior.Strict);
            Db = new SqliteConnection("DataSource=:memory:");
            var upgrader = DeployChanges.To
                .SQLiteDatabase(new SharedConnection(Db))
                .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
                .LogToNowhere()
                .Build();
            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                throw result.Error;
            }
            Db.Execute("DELETE FROM Station");
            Db.Execute("DELETE FROM KnownDomain");
            Db.Execute("DELETE FROM sqlite_sequence");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Db.Dispose();
        }
    }
}
