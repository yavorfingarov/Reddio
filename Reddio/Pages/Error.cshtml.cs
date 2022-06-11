namespace Reddio.Pages
{
    public class ErrorModel : PageModel
    {
        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
            if (Response.StatusCode == 200)
            {
                Response.StatusCode = 404;
            }
            ErrorMessage = Response.StatusCode switch
            {
                400 => "Bad Request",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unexpected Error"
            };
        }
    }
}
