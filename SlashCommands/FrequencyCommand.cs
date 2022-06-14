using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Frequency
    {
        public const string COMMAND_NAME = "frequency";
        public const string OPTION_MINUTES = "minutes";

        internal class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var frequency = new SlashCommandOptionBuilder()
                    .WithName(OPTION_MINUTES)
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Number)
                    .WithMinValue(0)
                    .WithDescription("The number of minutes between each draw of the roulette.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Set how frequent a roulette draw occurs.")
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

                var minutes = (double) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_MINUTES)?.Value;

                guildConfig.RouletteState.Frequency = TimeSpan.FromMinutes(minutes);
                guildConfig.RouletteState.NextCommodityAvailableAt = guildConfig.RouletteState.LatestCommodityPostedAt + guildConfig.RouletteState.Frequency;
                this._objectRepo.Upsert(guildConfig);

                await slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"New frequency set! ♥").Build(), ephemeral: true);
            }

        }

    }
}
