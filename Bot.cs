using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using FaceCord.Common;
using FaceCord.Discord;
using FaceCord.Facebook;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FaceCord
{
    public class Bot : BackgroundService
    {
        private readonly ILogger<Bot> logger;
        private readonly IDiscordClient discordClient;
        private readonly IFacebookService facebookService;
        private readonly IFacebookBrowser facebookBrowser;
        private readonly BotSettings botSettings;
        private readonly IDiscordChannelCommandHandler discordChannelCommandHandler;
        private readonly IHostApplicationLifetime applicationLifetime;

        private const long PollTimeInMin = 30; 

        public Bot(ILogger<Bot> logger,
            IDiscordClient discordClient,
            IFacebookService facebookService,
            BotSettings botSettings,
            IDiscordChannelCommandHandler discordChannelCommandHandler,
            IHostApplicationLifetime applicationLifetime, 
            IFacebookBrowser facebookBrowser)
        {
            this.logger = logger;
            this.discordClient = discordClient;
            this.facebookService = facebookService;
            this.botSettings = botSettings;
            this.discordChannelCommandHandler = discordChannelCommandHandler;
            this.applicationLifetime = applicationLifetime;
            this.facebookBrowser = facebookBrowser;
            this.applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
        }

        private async void OnApplicationStopped()
        {
            await facebookBrowser.DisposeAsync();
        }

        private void OnChannelMessageReceived(ISocketMessageChannel channel, string message)
        {
            try
            {
                if (!message.ToLower().StartsWith($"!{botSettings.Name}"))
                    return;

                discordChannelCommandHandler.HandleCommand(channel, message);
            }
            catch (Exception)
            {
                logger.LogWarning($"Failed to handle command: {message}");
            }
        }

        private async Task SendNewPostNotification(bool hasNewPost)
        {
            if (!hasNewPost)
                return;

            await discordClient.SendMessage(DiscordNewPostNotificationMessages.GetMessage());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Starting up [Bot]");
                
                await facebookService.Init();
                discordClient.SubscribeToChannelMessage(OnChannelMessageReceived);
                await discordClient.CreateInitialConnection(botSettings.Discord.Token);
                
                logger.LogInformation("Started [Bot]");
                while (!stoppingToken.IsCancellationRequested)
                {
                    var hasNewPost = await facebookService.UpdateCachedPosts();
                    await SendNewPostNotification(hasNewPost);

                    await Task.Delay((int)TimeSpan.FromMinutes(PollTimeInMin).TotalMilliseconds, stoppingToken);
                }

            }
            catch (Exception e)
            {
                logger.LogError(e, "Error:");
            }
            finally
            {
                await discordClient.Disconnect();
                logger.LogInformation("Stopped [Bot]");
                applicationLifetime.StopApplication();
            }
        }
    }
}
