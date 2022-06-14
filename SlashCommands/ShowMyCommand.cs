using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class ShowMy {

        public const string COMMAND_NAME = "showmy";
        public const string COMPONENT_NAME = "showmy";

        public class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Show a commodity from your deck!")
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

                var selectMenu = new SelectMenuBuilder().WithPlaceholder($"Which one of your {guildState.CommodityPluralTerm} would you like to show?").WithCustomId("showmy");
                foreach (var claim in deck)
                {
                    var commodity = commodities.FirstOrDefault(c => c.Id == claim.Key);
                    if (commodity == null) continue;
                    selectMenu.AddOption($"{commodity.Name} ({commodity.Rarity})", commodity.Id.ToString());
                }
                var component = new ComponentBuilder().WithSelectMenu(selectMenu).Build();
                await slashCommand.RespondAsync(components: component, ephemeral: true);
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
                var commodity = this._objectRepo.GetById<Commodity>(ulong.Parse(component.Data.Values.First()));
                await component.RespondAsync(embed: await commodity.ToEmbed(this._mediaRepo));
            }

        }

    }
}
