using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using FaceCord.Common;
using FaceCord.Facebook;

namespace FaceCord.Discord
{
    public interface IDiscordChannelCommandHandler
    {
        Task HandleCommand(ISocketMessageChannel channel, string message);
    }

    public class DiscordChannelCommandHandler : IDiscordChannelCommandHandler
    {

        private readonly BotSettings botSettings;
        private readonly IFacebookService facebookService;

        public DiscordChannelCommandHandler(BotSettings botSettings,
            IFacebookService facebookService)
        {
            this.botSettings = botSettings;
            this.facebookService = facebookService;
        }

        public async Task HandleCommand(ISocketMessageChannel channel, string message)
        {
            var commandArgs = GetCommandArgs(message);

            if (commandArgs == null)
                return;

            int? arg = commandArgs != string.Empty ? int.Parse(commandArgs) : null;
            var post = facebookService.GetPost(arg);
            var text = post.GetText();
            var textChunks = text.ChunksUpTo(1800);

            foreach (var textChunk in textChunks)
                await channel.SendMessageAsync(textChunk);
        }

        private static string GetCommandRegex(string botName) => $"!(?<bot_name>{botName})(?: (?=[0-9])(?<args>[0-9]))?";

        public string GetCommandArgs(string message)
        {
            var regex = Regex.Match(message, GetCommandRegex(botSettings.Name), RegexOptions.IgnoreCase);

            if (!regex.Success)
                return null;

            if (!regex.ToString().Equals(message, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var args = GetValueOrNull(regex.Groups["args"]);
            if (args == null)
                return string.Empty;

            return args;
        }

        private static string GetValueOrNull(Group group)
        {
            return group.Success ? group.Value : null;
        }
    }
}
