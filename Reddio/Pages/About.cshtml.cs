namespace Reddio.Pages
{
    public class AboutModel : PageModel
    {
        public IEnumerable<(string Name, int TrackCount)> Stations { get; set; } = new List<(string, int)>();

        public DateTime LastUpdate { get; set; }

        private readonly IDbConnection _Db;

        public AboutModel(IDbConnection db)
        {
            _Db = db;
        }

        public void OnGet()
        {
            Stations = _Db.Query<(string, int)>(
                "SELECT s.Name, COUNT(t.Id) " +
                "FROM Station s " +
                "JOIN Track t ON s.Id = t.StationId " +
                "GROUP BY s.Id " +
                "ORDER BY s.DisplayOrder");
            LastUpdate = _Db.QuerySingle<DateTime>("SELECT LastImport FROM Metadata");
        }
    }
}
