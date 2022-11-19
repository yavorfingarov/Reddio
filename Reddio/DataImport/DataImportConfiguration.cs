namespace Reddio.DataImport
{
    [Configuration("DataImport")]
    public class DataImportConfiguration
    {
        public int Period { get; set; }

        public int HostedServicePeriod { get; set; }
    }
}
