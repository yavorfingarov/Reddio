using Microsoft.AspNetCore.Mvc;

namespace Reddio.Pages
{
    public class ListenModel : PageModel
    {
        public string? Station { get; set; }

        private readonly IDbConnection _Db;

        public ListenModel(IDbConnection db)
        {
            _Db = db;
        }

        public IActionResult OnGet(string station)
        {
            Station = _Db.QuerySingleOrDefault<string?>("SELECT Name FROM Station WHERE Name = @Name", new { Name = station });
            if (Station == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
