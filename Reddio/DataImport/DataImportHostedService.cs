namespace Reddio.DataImport
{
    public sealed class DataImportHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DataImportHostedService> _Logger;

        private readonly IServiceProvider _ServiceProvider;

        private Timer? _Timer;

        public DataImportHostedService(ILogger<DataImportHostedService> logger, IServiceProvider serviceProvider)
        {
            _Logger = logger;
            _ServiceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimerCallback importData = async (state) =>
            {
                using var scope = _ServiceProvider.CreateScope();
                var dataImportHandler = scope.ServiceProvider.GetRequiredService<IDataImportHandler>();
                await ImportDataAsync(dataImportHandler);
            };
            _Timer = new Timer(importData, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        public async Task ImportDataAsync(IDataImportHandler dataImportHandler)
        {
            try
            {
                await dataImportHandler.HandleAsync();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Could not import data.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose() => _Timer?.Dispose();
    }
}
