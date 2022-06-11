namespace Reddio.Pages
{
    public class IndexModel : PageModel
    {
        public IEnumerable<string> Stations { get; set; } = new List<string>();

        private readonly IDbConnection _Db;

        public IndexModel(IDbConnection db)
        {
            _Db = db;
        }

        public void OnGet()
        {
            Stations = _Db.Query<string>("SELECT Name FROM Station ORDER BY DisplayOrder");
        }
    }
}
