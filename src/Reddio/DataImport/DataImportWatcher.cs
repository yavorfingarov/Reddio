namespace Reddio.DataImport
{
    public class DataImportWatcher : IMiddleware
    {
        public bool IsPerformingFreshImport { get; set; }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (IsPerformingFreshImport && context.Response.StatusCode == StatusCodes.Status200OK)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                return;
            }
            await next(context);
        }
    }
}
