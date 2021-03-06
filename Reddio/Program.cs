using DbUp;
using Microsoft.Data.Sqlite;
using NLog;
using NLog.Web;
using Reddio.Api;
using Reddio.DataImport;
using SimpleRequestLogger;

namespace Reddio
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = LogManager.Setup()
                .LoadConfigurationFromAppSettings()
                .GetCurrentClassLogger();
            try
            {
                logger.Info("Starting application.");
                var builder = WebApplication.CreateBuilder(args);
                ConfigureServices(builder);
                var app = builder.Build();
                logger.Debug($"Environment: {app.Environment.EnvironmentName}");
                MigrateDb(app);
                Configure(app);
                app.Run();
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "The application could not start.");
            }
            finally
            {
                logger.Info("Stopping application gracefully.");
                LogManager.Shutdown();
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();

            builder.Host.UseNLog();

            builder.Services.AddScoped<IDbConnection>(sp => new SqliteConnection(builder.Configuration.GetConnectionString("Default")));

            builder.Services.AddScoped<IDataImportHandler, DataImportHandler>();

            builder.Services.AddSingleton<DataImportWatcher>();

            builder.Services.AddHostedService<DataImportHostedService>();

            builder.Services.AddHttpClient<IRedditService, RedditService>();

            builder.Services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/Listen", "/r/{station}");
            });

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddHsts(options =>
                {
                    options.Preload = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge = TimeSpan.FromDays(365);
                });
            }
        }

        private static void Configure(WebApplication app)
        {
            app.UseRequestLogging();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), branch =>
            {
                branch.UseMiddleware<ExceptionHandler>();

                branch.UseMiddleware<DataImportWatcher>();

                branch.UseRouting();

                branch.UseEndpoints(endpoints =>
                {
                    endpoints.MapMethods("/api/health", new[] { "HEAD" }, HealthRequestHandler.Handle);

                    endpoints.MapPost("/api/queue", QueueRequestHandler.Handle);
                });
            });

            app.UseStaticFiles();

            app.UseStatusCodePagesWithReExecute("/Error");

            app.UseMiddleware<DataImportWatcher>();

            app.MapRazorPages();
        }

        private static void MigrateDb(WebApplication app)
        {
            var dataDirectory = app.Configuration["DataDirectory"];
            Directory.CreateDirectory(dataDirectory);
            AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
            var upgrader = DeployChanges.To
                .SQLiteDatabase(app.Configuration.GetConnectionString("Default"))
                .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
                .LogToNowhere()
                .Build();
            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                throw result.Error;
            }
        }
    }
}
