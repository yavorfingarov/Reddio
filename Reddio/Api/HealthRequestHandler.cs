namespace Reddio.Api
{
    public class HealthRequestHandler
    {
        public static IResult Handle(IDbConnection db, IConfiguration configuration)
        {
            var lastImport = db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
            if (DateTime.UtcNow - lastImport > TimeSpan.FromHours((int.Parse(configuration["DataImportPeriod"]) * 2) + 1.5))
            {
                return Results.StatusCode(500);
            }

            return Results.StatusCode(200);
        }
    }
}
