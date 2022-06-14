using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Undelete
    {

        public const string COMMAND_NAME = "Undelete";
        public const string OPTION_COMMODITY = "commodity";

        internal class CommandFactory : ICommandFactory
        {
            private readonly IObjectRepository _objectRepo;

            public CommandFactory(IObjectRepository objectRepo) =>
                this._objectRepo = objectRepo;


            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                IQueryable<Commodity> commodities = new List<Commodity>().AsQueryable();
                if (guildContext != null)
                    commodities = this._objectRepo.GetAll<Commodity>()
                        .Where(c => c.GuildId == guildContext.Id && c.Deleted);

                var commodityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_COMMODITY)
                    .WithRequired(true)
                    .WithDescription("The commodity to delete from the roulette.")
                    .WithType(ApplicationCommandOptionType.String);

                foreach (var commodity in commodities)
                    commodityOption.AddChoice(commodity.Name, commodity.Id.ToString());

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Delete a commodity from the roulette, this will not remove it from users who have already claimed it.")
                    .AddOption(commodityOption)
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

            public Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var targetCommodityId = ulong.Parse((string)slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_COMMODITY)?.Value);
                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;
                var guildState = this._objectRepo.GetById<GuildState>(guildId);

                var commodity = this._objectRepo.GetById<Commodity>(targetCommodityId);
                commodity.Deleted = false;
                this._objectRepo.Upsert(commodity);

                var embed = new EmbedBuilder()
                            .WithDescription($"The **{commodity.Name}** {guildState.CommoditySingularTerm} has been deleted and is no longer available. Those who have already claimed it will still have it.")
                            .Build();

                return slashCommand.RespondAsync(embed: embed, ephemeral: true);
            }

        }

    }
}
