using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class ClaimLimit
    {
        public const string COMMAND_NAME = "claimlimit";
        public const string OPTION_LIMIT = "limit";

        internal class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var frequency = new SlashCommandOptionBuilder()
                    .WithName(OPTION_LIMIT)
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Number)
                    .WithMinValue(0)
                    .WithDescription("The number of times a draw can be claimed.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Set how many times a draw can be claimed before rejected new claims.")
                    .AddOption(frequency)
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }

        }

        internal class CommandHandler : ICommandHandler
        {
            private readonly IObjectRepository _objectRepo;

            public CommandHandler(IObjectRepository _objectRepo)
            {
                this._objectRepo = _objectRepo;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;
                var guildConfig = this._objectRepo.GetById<GuildState>(guildId);

                var limit = (double) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_LIMIT)?.Value;

                if (limit < 0)
                {
                    limit = 0;
                }

                guildConfig.RouletteState.ClaimLimit = (int) limit;
                this._objectRepo.Upsert(guildConfig);

                if (limit == 0)
                    await slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"Okay, I won't limit number of claims. 🙂").Build(), ephemeral: false);
                else
                    await slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"New claim limit set! ♥").Build(), ephemeral: false);
            }

        }

    }
}
