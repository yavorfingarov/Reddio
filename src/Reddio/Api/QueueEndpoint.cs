﻿namespace Reddio.Api
{
    [Endpoint]
    public class QueueEndpoint
    {
        [Post("/api/queue")]
        public static IResult Handle(QueueRequest request, IDbConnection db, QueueConfiguration queueConfiguration)
        {
            var queue = GetQueue(db, request.Station, queueConfiguration.Length, request.IgnoreThreadIds);
            if (!queue.Any())
            {
                queue = GetQueue(db, request.Station, queueConfiguration.Length, Enumerable.Empty<string>());
            }
            if (!queue.Any())
            {
                return Results.BadRequest();
            }

            return Results.Ok(queue);
        }

        private static IEnumerable<Track> GetQueue(IDbConnection db, string stationName, int queueLength, IEnumerable<string> ignoreThreadIds)
        {
            var query =
                "SELECT t.ThreadId, t.Title, t.Url " +
                "FROM Track t " +
                "JOIN Station s ON s.Id = t.StationId " +
                "WHERE {0} " +
                "ORDER BY RANDOM() " +
                "LIMIT @QueueLength";
            var predicates = new List<string>() { "s.Name = @StationName" };
            var parameters = new Dictionary<string, object?>()
            {
                ["StationName"] = stationName,
                ["QueueLength"] = queueLength
            };
            if (ignoreThreadIds != null && ignoreThreadIds.Any())
            {
                predicates.Add("t.ThreadId NOT IN @IgnoreThreadIds");
                parameters["IgnoreThreadIds"] = ignoreThreadIds;
            }
            var queue = db.Query<Track>(string.Format(CultureInfo.InvariantCulture, query, string.Join(" AND ", predicates)), parameters);

            return queue;
        }
    }

    public record QueueRequest(string Station, IEnumerable<string> IgnoreThreadIds);

    public record Track(string ThreadId, string Title, string Url);

    [Configuration("Queue")]
    public class QueueConfiguration
    {
        public int Length { get; set; }
    }
}
