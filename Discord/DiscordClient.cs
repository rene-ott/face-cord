using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FaceCord.Common;

namespace FaceCord.Discord
{
    public interface IDiscordClient
    {
        Task CreateInitialConnection(string token);
        Task Disconnect();
        void SubscribeToChannelMessage(Action<ISocketMessageChannel, string> action);
        Task SendMessage(string msg);
    }

    public class DiscordClient : IDiscordClient, IDisposable
    {
        private readonly DiscordSocketClient client;
        private Func<SocketMessage, Task> channelMessageCallback;
        private readonly BotSettings botSettings;

        public DiscordClient(BotSettings botSettings)
        {
            this.botSettings = botSettings;
            client = new DiscordSocketClient();
        }


        public async Task CreateInitialConnection(string token)
        {
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();
        }

        public async Task Disconnect()
        {
            await client.LogoutAsync();
            await client.StopAsync();
        }

        public void SubscribeToChannelMessage(Action<ISocketMessageChannel, string> action)
        {
            channelMessageCallback = message =>
            {
                action(message.Channel, message.Content);
                return Task.CompletedTask;
            };

            client.MessageReceived += channelMessageCallback;
        }

        public async Task SendMessage(string msg)
        {
            var configuredGuildChannelPairs = botSettings.Channels
                .Select(x => x.Split(":"))
                .Select(x => new { GuildId = ulong.Parse(x[0]), ChannelId = ulong.Parse(x[1]) })
                .ToList();

            foreach (var pair in configuredGuildChannelPairs)
            {
                var channel = client.GetGuild(pair.GuildId)?.GetTextChannel(pair.ChannelId);
                if (channel == null)
                    continue;

                await channel.SendMessageAsync(msg);
            }
        }

        public void Dispose()
        {
            if (channelMessageCallback != null)
                client.MessageReceived -= channelMessageCallback;

            client.Dispose();
        }
    }
}
