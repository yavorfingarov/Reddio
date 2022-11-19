using Reddio.DataImport;

namespace Reddio.Api
{
    [Endpoint]
    public class HealthEndpoint
    {
        [Map("HEAD", "/api/health")]
        public static IResult Handle(IDbConnection db, DataImportConfiguration dataImportConfiguration)
        {
            var lastImport = db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
            var limit = TimeSpan.FromHours((dataImportConfiguration.Period * 2) + (dataImportConfiguration.HostedServicePeriod * 1.5));
            if (DateTime.UtcNow - lastImport > limit)
            {
                return Results.StatusCode(500);
            }

            return Results.StatusCode(200);
        }
    }
}
