using System.Threading;
using System.Timers;

namespace Reddio.DataImport
{
    public sealed class DataImportBackgroundService : BackgroundService
    {
        private readonly ILogger<DataImportBackgroundService> _Logger;

        private readonly DataImportConfiguration _DataImportConfiguration;

        private readonly IServiceScopeFactory _ServiceScopeFactory;

        public DataImportBackgroundService(ILogger<DataImportBackgroundService> logger,
            DataImportConfiguration dataImportConfiguration,
            IServiceScopeFactory serviceScopeFactory)
        {
            _Logger = logger;
            _DataImportConfiguration = dataImportConfiguration;
            _ServiceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                using var scope = _ServiceScopeFactory.CreateScope();
                var dataImportHandler = scope.ServiceProvider.GetRequiredService<IDataImportHandler>();
                try
                {
                    await dataImportHandler.HandleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _Logger.LogError(ex, "Could not import data.");
                }
            } while (await IsWaiting(stoppingToken));
        }

        private async Task<bool> IsWaiting(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(_DataImportConfiguration.HostedServicePeriod), cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}
