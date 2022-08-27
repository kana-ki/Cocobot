using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class DeckList {

        public const string COMMAND_NAME = "decklist";
        public const string OPTION_PRIVATE = "private";

        public class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                var privateOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_PRIVATE)
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Boolean)
                    .WithDescription("Show your deck only to you and not to others in the channel.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Show your deck and see what you've collected!")
                    .AddOption(privateOption)
                    .Build();
            }

        }

        public class CommandHandler : ICommandHandler
        {

            private readonly IObjectRepository _objectRepo;

            public CommandHandler(IObjectRepository objectRepo)
            {
                this._objectRepo = objectRepo;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guild = ((SocketTextChannel) slashCommand.Channel).Guild;
                var guildConfig = this._objectRepo.GetById<GuildState>(guild.Id);
                var @private = (bool) (slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_PRIVATE)?.Value ?? false);
                var deck = this._objectRepo.GetWhere<Model.Claim>(c => c.GuildId == guild.Id && c.UserId == slashCommand.User.Id)
                                            .GroupBy(c => c.CommodityId);
                var commodities = this._objectRepo.GetById<Commodity>(deck.Select(c => c.Key)).Where(c => !(c is null)).OrderByDescending(c => c.Rarity);

                var stringBuilder = new StringBuilder();
                foreach (var commodity in commodities)
                {
                    var claim = deck.FirstOrDefault(c => c.Key == commodity.Id);
                    if (commodity == null) continue;
                    stringBuilder.Append(claim.Count() + " x ");
                    stringBuilder.AppendLine(commodity.ToString());
                }

                var embed = new EmbedBuilder()
                                .WithTitle($"{slashCommand.User.Username}'s {guildConfig.CommodityPluralTerm}")
                                .WithDescription(stringBuilder.ToString())
                                .Build();

                await slashCommand.RespondAsync(embed: embed);
            }

        }

    }
}
