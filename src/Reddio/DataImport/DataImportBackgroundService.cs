namespace Reddio.DataImport
{
    public sealed class DataImportBackgroundService : BackgroundService
    {
        private readonly ILogger<DataImportBackgroundService> _Logger;

        private readonly DataImportConfiguration _DataImportConfiguration;

        private readonly IServiceProvider _ServiceProvider;

        public DataImportBackgroundService(ILogger<DataImportBackgroundService> logger,
            DataImportConfiguration dataImportConfiguration,
            IServiceProvider serviceProvider)
        {
            _Logger = logger;
            _DataImportConfiguration = dataImportConfiguration;
            _ServiceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(_DataImportConfiguration.HostedServicePeriod));
            do
            {
                using var scope = _ServiceProvider.CreateScope();
                var dataImportHandler = scope.ServiceProvider.GetRequiredService<IDataImportHandler>();
                await ImportDataAsync(dataImportHandler, stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
        }

        public async Task ImportDataAsync(IDataImportHandler dataImportHandler, CancellationToken cancellationToken)
        {
            try
            {
                await dataImportHandler.HandleAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Could not import data.");
            }
        }
    }
}
