using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class List
    {

        public const string COMMAND_NAME = "list";
        public const string OPTION_SHOW_RESERVED = "showreserved";

        internal class CommandFactory : ICommandFactory
        {
            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var showReservedOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_SHOW_RESERVED)
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Boolean)
                    .WithDescription("Show reserved cards in the response.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("List all of the commodities in your roulette.")
                    .AddOption(showReservedOption)
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

            public Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var showReserved = (bool) (slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_SHOW_RESERVED)?.Value ?? false);

                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;
                var guildState = this._objectRepo.GetById<GuildState>(guildId);
                var commodities = this._objectRepo.GetWhere<Commodity>(c => c.GuildId == guildId);
                if (!showReserved)
                    commodities = commodities.Where(c => !c.Limited);

                if (!commodities.Any())
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"There are no {guildState.CommodityPluralTerm} in your roulette yet.").Build());

                var stringBuilder = new StringBuilder();
                foreach (var commodity in commodities)
                    stringBuilder.AppendLine(commodity.ToString());

                var embed = new EmbedBuilder()
                    .WithTitle(guildState.CommodityPluralTerm + " available in the roulette")
                    .WithDescription(stringBuilder.ToString())
                    .Build();

                return slashCommand.RespondAsync(embed: embed);
            }

        }

    }
}
