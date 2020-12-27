using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using KuroEncoder.Extensions;
using KuroEncoder.Models;
using KuroEncoder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace KuroEncoder
{
    public class Program
    {
        private const String DefaultOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";

        public static async Task Main(String[] args)
        {
            // Create a logger until the host is built.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient.Default.LogicalHandler", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient.Default.ClientHandler", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: SystemConsoleTheme.Colored, outputTemplate: DefaultOutputTemplate,
                    applyThemeToRedirectedOutput: true)
                .CreateBootstrapLogger();

            TaskScheduler.UnobservedTaskException += (staate, e) =>
            {
                e.SetObserved();
                Log.ForContext<Program>()
                    .Fatal(e.Exception, "There was an unobserved exception");
                Debugger.Break();
            };

            try
            {
                Log.ForContext<Program>()
                    .Information("Building Host");
                using var host = CreateHostBuilder(args)
                    .Build();

                Log.ForContext<Program>()
                    .Information("Starting Kuro Encoder");
                await host.StartAsync();

                Log.ForContext<Program>()
                    .Information("Kuro Encoder startup finished, waiting for shutdown");
                await host.WaitForShutdownAsync();

                Log.ForContext<Program>()
                    .Information("Kuro Encoder has shut down");
            }
            catch (OptionsValidationException crap)
            {
                var yourAFailure = crap.Failures.ToArray();
                if (yourAFailure.Length == 0)
                {
                    // Well... CRAP!
                    throw new Exception("There was a validation error, but no failures provided.", crap);
                }

                Log.ForContext<Program>()
                    .Fatal("There was {count} configuration errors. Please correct these via cli / appsettings.json",
                        yourAFailure.Length);

                foreach (var failure in yourAFailure)
                {
                    Log.ForContext<Program>()
                        .Error("{failure}", failure);
                }
            }
            catch (Exception crap)
            {
                Log.ForContext<Program>()
                    .Fatal(crap, "There was an unexpected exception,");
                Debugger.Break();
            }
        }

        private static IHostBuilder CreateHostBuilder(String[] args)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, b) =>
                {
                    b.AddCommandLine<EncoderOptions>(args);
                })
                .ConfigureServices((ctx, s) =>
                {
                    s.AddSingleton<EncoderService>();
                    s.AddSingleton<IHostedService, EncoderService>(s => s.GetRequiredService<EncoderService>());

                    s.ConfigureOptions<EncoderOptions>();

                    s.Configure<EncoderOptions>(ctx.Configuration);
                })
                .UseSerilog((ctx, s) => s.ReadFrom.Configuration(ctx.Configuration))
                .UseStashbox(s =>
                    s.Configure(c =>
                        c.WithUnknownTypeResolution()
                            .WithDisposableTransientTracking()))
                .UseConsoleLifetime();
        }
    }
}
