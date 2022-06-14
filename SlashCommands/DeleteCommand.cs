using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Delete
    {

        public const string COMMAND_NAME = "delete";
        public const string OPTION_COMMODITY = "commodity";
        public const string OPTION_REVOKE = "revoke";

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
                        .Where(c => c.GuildId == guildContext.Id && !c.Deleted);

                var commodityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_COMMODITY)
                    .WithRequired(true)
                    .WithDescription("The commodity to delete from the roulette.")
                    .WithType(ApplicationCommandOptionType.String);

                foreach (var commodity in commodities)
                    commodityOption.AddChoice(commodity.Name, commodity.Id.ToString());

                var revokeOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_REVOKE)
                    .WithRequired(false)
                    .WithDescription("Revoke the commodity from everyone who has claimed it or been awarded it.")
                    .WithType(ApplicationCommandOptionType.Boolean);

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Delete a commodity from the roulette.")
                    .AddOption(commodityOption)
                    .AddOption(revokeOption)
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }
        }

        internal class CommandHandler : ICommandHandler
        {
            private readonly IObjectRepository _objectRepo;
            private readonly IMediaRepository _mediaRepo;
            private readonly ICommandBroker _commandBroker;

            public CommandHandler(IObjectRepository _objectRepo, IMediaRepository _mediaRepo, ICommandBroker commandBroker)
            {
                this._objectRepo = _objectRepo;
                this._mediaRepo = _mediaRepo;
                this._commandBroker = commandBroker;
            }

            public Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var targetCommodityId = ulong.Parse((string)slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_COMMODITY)?.Value);
                var revoke = (bool)(slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_REVOKE)?.Value ?? false);
                var commodity = this._objectRepo.GetById<Commodity>(targetCommodityId);
                var guild = ((SocketTextChannel)slashCommand.Channel).Guild;
                var guildState = this._objectRepo.GetById<GuildState>(guild.Id);
                var claims = this._objectRepo.GetWhere<Model.Claim>(c => c.CommodityId == targetCommodityId);

                if (revoke)
                {
                    foreach (var claim in claims)
                        this._objectRepo.Delete(claim);
                    this._objectRepo.Delete(commodity);
                    this._mediaRepo.Delete(commodity.ImageKey);
                
                    _ = this._commandBroker.RegisterAllAsync(guild);

                    var embed = new EmbedBuilder()
                        .WithDescription($"The **{commodity.Name}** {guildState.CommoditySingularTerm} has been revoked from everyone and deleted.")
                        .Build();
                    return slashCommand.RespondAsync(embed: embed, ephemeral: true);
                }
                else
                {
                    if (claims.Any()) 
                    {
                        commodity.Deleted = true;
                        this._objectRepo.Upsert(commodity);

                        _ = this._commandBroker.RegisterAllAsync(guild);

                        var embed = new EmbedBuilder()
                                    .WithDescription($"The **{commodity.Name}** {guildState.CommoditySingularTerm} has been deleted and is no longer available. Those who have already claimed it will still have it.")
                                    .Build();

                        return slashCommand.RespondAsync(embed: embed, ephemeral: true);
                    }
                    else
                    {
                        this._objectRepo.Delete(commodity);
                        this._mediaRepo.Delete(commodity.ImageKey);

                        _ = this._commandBroker.RegisterAllAsync(guild);

                        var embed = new EmbedBuilder()
                                    .WithDescription($"The **{commodity.Name}** {guildState.CommoditySingularTerm} has been deleted and is no longer available.")
                                    .Build();

                        return slashCommand.RespondAsync(embed: embed, ephemeral: true);
                    }
                }
            }

        }

    }
}
