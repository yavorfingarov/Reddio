namespace Reddio.Api
{
    [Endpoint]
    public class HealthEndpoint
    {
        [Map("HEAD", "/api/health")]
        public static IResult Handle()
        {
            return Results.StatusCode(200);
        }
    }
}
