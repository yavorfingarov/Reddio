namespace Reddio.Api
{
    public class ExceptionHandler
    {
        private readonly RequestDelegate _Next;

        private readonly ILogger<ExceptionHandler> _Logger;

        public ExceptionHandler(RequestDelegate next, ILogger<ExceptionHandler> logger)
        {
            _Next = next;
            _Logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _Next(context);
            }
            catch (BadHttpRequestException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                _Logger.LogError(ex, "An unhandled exception has occurred while executing the request.");
            }
        }
    }
}
