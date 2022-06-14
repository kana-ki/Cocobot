using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Disable {

        public const string COMMAND_NAME = "disable";

        public class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Enabled the roulette in this channel.")
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }

        }

        public class CommandHandler : ICommandHandler
        {
            private readonly IRouletteRunner _rouletteRunner;

            public CommandHandler(IRouletteRunner rouletteRunner)
            {
                this._rouletteRunner = rouletteRunner;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var channel = (SocketTextChannel) slashCommand.Channel;
                this._rouletteRunner.Disable(channel);
                await slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"The roulette will now happen in this channel! ♥").Build(), ephemeral: true);
            }

        }

    }
}
