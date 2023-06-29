using DbUp;
using Microsoft.Data.Sqlite;
using NLog;
using NLog.Web;
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
                app.Logger.LogDebug("Environment: {Environment}", app.Environment.EnvironmentName);
                Configure(app);
                MigrateDb(app);
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

            builder.ConfigureServices();

            builder.Services.AddScoped<IDbConnection>(sp => new SqliteConnection(builder.Configuration.GetConnectionString("Default")));

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

            app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), apiBranch =>
            {
                apiBranch.UseExceptionHandling();

                apiBranch.UseHttpsRedirection();

                apiBranch.UseRouting();

                apiBranch.UseEndpoints(endpoints => endpoints.MapEndpoints());
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");

                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseStatusCodePagesWithReExecute("/Error");

            app.MapRazorPages();
        }

        private static void MigrateDb(WebApplication app)
        {
            var dataDirectory = app.Configuration["DataDirectory"];
            ArgumentNullException.ThrowIfNull(dataDirectory);
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
