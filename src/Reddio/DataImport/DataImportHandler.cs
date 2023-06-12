namespace Reddio.DataImport
{
    public interface IDataImportHandler
    {
        Task HandleAsync(CancellationToken cancellationToken);
    }

    public class DataImportHandler : IDataImportHandler
    {
        private readonly IDbConnection _Db;

        private readonly IRedditService _RedditService;

        private readonly DataImportWatcher _DataImportWatcher;

        private readonly DataImportConfiguration _DataImportConfiguration;

        private readonly ILogger<DataImportHandler> _Logger;

        public DataImportHandler(IDbConnection db, IRedditService redditService, DataImportWatcher dataImportWatcher,
            DataImportConfiguration dataImportConfiguration, ILogger<DataImportHandler> logger)
        {
            _Db = db;
            _RedditService = redditService;
            _DataImportWatcher = dataImportWatcher;
            _DataImportConfiguration = dataImportConfiguration;
            _Logger = logger;
        }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            var lastUpdate = _Db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
            if (DateTime.UtcNow - lastUpdate < TimeSpan.FromHours(_DataImportConfiguration.Period) - TimeSpan.FromMinutes(5))
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
                _Logger.LogDebug("Fetching listing for {Station}.", station);
                if (station.TrackCount == 0)
                {
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 300, "best", "all", cancellationToken));
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 200, "best", "year", cancellationToken));
                    tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 100, "best", "month", cancellationToken));
                }
                tracks.AddRange(await GetTracksAsync(station.Name, station.Id, 50, "hot", null, cancellationToken));
            }
            ImportTracks(tracks);
            _DataImportWatcher.IsPerformingFreshImport = false;
        }

        private async Task<IEnumerable<Track>> GetTracksAsync(string stationName,
            int stationId, int count, string sort, string? period, CancellationToken cancellationToken)
        {
            var commentThreads = await _RedditService.GetListingAsync(stationName, count, sort, period, cancellationToken);
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
                if (knownDomains.Any(kd => track.Url.StartsWith(kd, StringComparison.InvariantCulture)))
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
