using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using moment.net;

namespace Cocobot.SlashCommands
{
    public static class Deck {

        public const string COMMAND_NAME = "deck";
        public const string COMPONENT_NAME = "deck";

        public class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Show the commodities in your deck!")
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
                var guildState = this._objectRepo.GetById<GuildState>(guild.Id);
                var deck = this._objectRepo.GetWhere<Model.Claim>(c => c.GuildId == guild.Id && c.UserId == slashCommand.User.Id)
                                            .GroupBy(c => c.CommodityId);
                var commodities = this._objectRepo.GetById<Commodity>(deck.Select(c => c.Key));

                var selectMenu = new SelectMenuBuilder().WithPlaceholder($"{slashCommand.User.Username}'s {guildState.CommodityPluralTerm}").WithCustomId($"{COMPONENT_NAME}:{guild.Id}:{slashCommand.User.Id}");
                foreach (var claim in deck)
                {
                    var commodity = commodities.FirstOrDefault(c => c.Id == claim.Key);
                    if (commodity == null) continue;

                    var description = new StringBuilder();
                    description.Append(commodity.Rarity);
                    description.Append(". ");
                    if (commodity.Limited)
                        description.Append(" Limited edition. Available by award only.");
                    description.Append(claim.Count());
                    description.Append(" in deck.");

                    selectMenu.AddOption($"{commodity.Name}", commodity.Id.ToString(), description.ToString());
                }
                var component = new ComponentBuilder().WithSelectMenu(selectMenu).Build();
                await slashCommand.RespondAsync(components: component, ephemeral: false);
            }

        }

        public class ComponentHandler : IComponentHandler
        {
            private readonly IObjectRepository _objectRepo;
            private readonly IMediaRepository _mediaRepo;

            public ComponentHandler(IObjectRepository objectRepo, IMediaRepository mediaRepo)
            {
                this._objectRepo = objectRepo;
                this._mediaRepo = mediaRepo;
            }

            public async Task HandleAsync(SocketMessageComponent component)
            {
                var guildId = ulong.Parse(component.Data.CustomId.Split(":")[1]);
                var userId = ulong.Parse(component.Data.CustomId.Split(":")[2]);
                var commodity = this._objectRepo.GetById<Commodity>(ulong.Parse(component.Data.Values.First()));

                var guildState = this._objectRepo.GetById<GuildState>(guildId);
                var commoditySingular = guildState?.CommoditySingularTerm ?? "commodity";

                var claims = this._objectRepo.GetWhere<Model.Claim>(c => c.GuildId == guildId && c.UserId == userId && c.CommodityId == commodity.Id);

                var embed = await commodity.ToEmbed(this._mediaRepo);

                var description = new StringBuilder();
                description.AppendLine(embed.Description);

                foreach (var claim in claims)
                {
                    description.Append(MentionUtils.MentionUser(userId));
                    description.Append(" ");
                    description.Append(claim.Type == ClaimType.Claim ? "claimed": "was awarded");
                    description.Append(" this ");
                    description.Append(commoditySingular);
                    description.Append(" ");
                    description.Append(claim.Claimed.FromNow());
                    description.AppendLine(".");
                }
                

                await component.UpdateAsync(async c => c.Embed = embed.WithDescription(description.ToString()).Build());
            }

        }

    }
}
