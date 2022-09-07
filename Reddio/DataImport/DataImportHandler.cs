namespace Reddio.DataImport
{
    public interface IDataImportHandler
    {
        Task HandleAsync();
    }

    public class DataImportHandler : IDataImportHandler
    {
        private readonly IDbConnection _Db;

        private readonly IRedditService _RedditService;

        private readonly DataImportWatcher _DataImportWatcher;

        private readonly IConfiguration _Configuration;

        private readonly ILogger<DataImportHandler> _Logger;

        public DataImportHandler(IDbConnection db, IRedditService redditService, DataImportWatcher dataImportWatcher,
            IConfiguration configuration, ILogger<DataImportHandler> logger)
        {
            _Db = db;
            _RedditService = redditService;
            _DataImportWatcher = dataImportWatcher;
            _Configuration = configuration;
            _Logger = logger;
        }

        public async Task HandleAsync()
        {
            var lastUpdate = _Db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
            var dataImportPeriod = int.Parse(_Configuration["DataImport:Period"]);
            if (DateTime.UtcNow - lastUpdate < TimeSpan.FromHours(dataImportPeriod) - TimeSpan.FromMinutes(5))
            {
                return;
            }
            _Logger.LogDebug("Importing data...");
            var stations = _Db.Query<(int Id, string Name, int TrackCount)>(
                "SELECT s.Id, s.Name, COUNT(t.Id) AS TrackCount " +
                "FROM Station s " +
                "LEFT JOIN Track t ON t.StationId = s.Id " +
                "GROUP BY s.Id");
            _DataImportWatcher.IsPerformingFreshImport = stations.Sum(s => s.TrackCount) == 0;
            var tracks = new List<Track>();
            foreach (var station in stations)
            {
                if (station.TrackCount == 0)
                {
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 500, "best", "all"));
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 300, "best", "year"));
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 200, "best", "month"));
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 100, "hot"));
                }
                else
                {
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 50, "hot"));
                }
            }
            ImportTracks(tracks);
            _DataImportWatcher.IsPerformingFreshImport = false;
        }

        private async Task<IEnumerable<Track>> GetTracksAsync(string stationName,
            int stationId, int count, string sort, string? period = null)
        {
            var commentThreads = await _RedditService.GetListingAsync(stationName, count, sort, period);
            var tracks = commentThreads
                .Select(t => new Track(stationId, t.Id, t.Title, t.Url))
                .Reverse();

            return tracks;
        }

        private void ImportTracks(IEnumerable<Track> tracks)
        {
            var knownDomains = _Db.Query<string>("SELECT Domain FROM KnownDomain")
                .SelectMany(kd => new[] { $"https://{kd}", $"http://{kd}" })
                .ToList();
            _Db.Open();
            using var transaction = _Db.BeginTransaction();
            try
            {
                var affectedRows = InsertData(transaction, tracks, knownDomains);
                ValidateImport(transaction);
                _Db.Execute("UPDATE Metadata SET LastImport = @LastImport",
                    new { LastImport = DateTime.UtcNow }, transaction);
                transaction.Commit();
                _Logger.LogDebug("Data import finished. {AffectedRows} row(s) affected.", affectedRows);
            }
            catch (Exception)
            {
                transaction.Rollback();

                throw;
            }
        }

        private int InsertData(IDbTransaction transaction, IEnumerable<Track> tracks, List<string> knownDomains)
        {
            var affectedRows = 0;
            foreach (var track in tracks)
            {
                if (knownDomains.Any(kd => track.Url.StartsWith(kd)))
                {
                    affectedRows += _Db.Execute(
                        "INSERT INTO Track (StationId, ThreadId, Title, Url) " +
                        "VALUES (@StationId, @ThreadId, @Title, @Url)",
                        track, transaction);
                }
            }

            return affectedRows;
        }

        private void ValidateImport(IDbTransaction transaction)
        {
            var emptyStationNames = _Db.Query<string>(
                "SELECT s.Name " +
                "FROM Station s " +
                "LEFT JOIN Track t ON t.StationId = s.Id " +
                "GROUP BY s.Id " +
                "HAVING COUNT(t.Id) = 0",
                transaction: transaction);
            if (emptyStationNames.Any())
            {
                throw new InvalidOperationException($"No tracks found for {string.Join(", ", emptyStationNames)}.");
            }
        }
    }

    public record Track(int StationId, string ThreadId, string Title, string Url);
}
