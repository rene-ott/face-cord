using System;
using FaceCord.Common;
using FaceCord.Discord;
using FaceCord.Facebook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FaceCord
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                const string logFile = "log.txt";
                const string template = "[{Timestamp:dd-MM-yyyy HH:mm:ss}][{Level:u3}] {Message}{NewLine}{Exception}";

                Files.DeleteIfExists(logFile);

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(logFile, outputTemplate: template)
                    .WriteTo.Console(outputTemplate: template)
                    .Enrich.FromLogContext()
                    .CreateLogger();

                Log.Information("Starting up [Application] v1");

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Start-up failed for [Application]");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddJsonFile("appsettings.Local.json", optional: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);

                    var botSettings = new BotSettings();
                    ctx.Configuration.Bind("Bot", botSettings);
                    services.AddSingleton(botSettings);

                    services.AddMemoryCache();

                    services.AddSingleton<IFacebookService, FacebookService>();
                    services.AddSingleton<IFacebookBrowser, FacebookBrowser>();

                    services.AddSingleton<IDiscordChannelCommandHandler, DiscordChannelCommandHandler>();
                    services.AddSingleton<IDiscordClient, DiscordClient>();
                    
                    services.AddHostedService<Bot>();
                });
    }
}
