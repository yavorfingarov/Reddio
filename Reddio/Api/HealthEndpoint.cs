namespace Reddio.Api
{
    [Endpoint]
    public class HealthEndpoint
    {
        [Map("HEAD", "/api/health")]
        public static IResult Handle(IDbConnection db, IConfiguration configuration)
        {
            var lastImport = db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
            var importPeriod = int.Parse(configuration["DataImport:Period"]);
            var hostedServicePeriod = int.Parse(configuration["DataImport:HostedServicePeriod"]);
            if (DateTime.UtcNow - lastImport > TimeSpan.FromHours((importPeriod * 2) + (hostedServicePeriod * 1.5)))
            {
                return Results.StatusCode(500);
            }

            return Results.StatusCode(200);
        }
    }
}
