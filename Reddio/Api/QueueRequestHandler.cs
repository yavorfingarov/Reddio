namespace Reddio.Api
{
    public class QueueRequestHandler
    {
        public static IResult Handle(QueueRequest request, IDbConnection db, IConfiguration configuration)
        {
            var stationId = db.QuerySingleOrDefault<int?>("SELECT Id FROM Station WHERE Name = @Name", new { Name = request.Station });
            if (stationId == null)
            {
                return Results.BadRequest();
            }
            var queueLength = int.Parse(configuration["QueueLength"]);
            var queue = GetQueue(db, stationId, queueLength, request.IgnoreThreadIds);
            if (!queue.Any())
            {
                queue = GetQueue(db, stationId, queueLength, Enumerable.Empty<string>());
            }

            return Results.Ok(queue);
        }

        private static IEnumerable<Track> GetQueue(IDbConnection db, int? stationId, int queueLength, IEnumerable<string> ignoreThreadIds)
        {
            var query = "SELECT ThreadId, Title, Url FROM Track WHERE {0} ORDER BY Id DESC LIMIT @QueueLength";
            var predicates = new List<string>() { "StationId = @StationId" };
            var parameters = new Dictionary<string, object?>()
            {
                ["StationId"] = stationId,
                ["QueueLength"] = queueLength
            };
            if (ignoreThreadIds.Any())
            {
                predicates.Add("ThreadId NOT IN @IgnoreThreadIds");
                parameters["IgnoreThreadIds"] = ignoreThreadIds;
            }
            var queue = db.Query<Track>(string.Format(query, string.Join(" AND ", predicates)), parameters);

            return queue;
        }
    }

    public record QueueRequest(string Station, IEnumerable<string> IgnoreThreadIds);

    public record Track(string ThreadId, string Title, string Url);
}
