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
            Stations = _Db.Query<string>(
                "SELECT s.Name " +
                "FROM Station s " +
                "WHERE EXISTS (SELECT * FROM Track t WHERE t.StationId = s.Id) " +
                "ORDER BY s.DisplayOrder");
        }
    }
}
