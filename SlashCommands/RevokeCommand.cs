using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Revoke
    {

        public const string COMMAND_NAME = "revoke";
        public const string OPTION_COMMODITY = "commodity";
        public const string OPTION_MENTIONABLE = "users";

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
                        .Where(c => c.GuildId == guildContext.Id);

                var commodityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_COMMODITY)
                    .WithRequired(true)
                    .WithDescription("The commodity to revoke from the user/role.")
                    .WithType(ApplicationCommandOptionType.String);

                foreach (var commodity in commodities)
                {
                    var name = commodity.Name;
                    if (commodity.Deleted)
                        name += " (Deleted)";
                    commodityOption.AddChoice(name, commodity.Id.ToString());
                }

                var mentionableOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_MENTIONABLE)
                    .WithRequired(true)
                    .WithDescription("The user or role of users to revoke the commodity from.")
                    .WithType(ApplicationCommandOptionType.Mentionable);

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Revoke a commodity from a role/user so it's no longer in their deck.")
                    .AddOption(commodityOption)
                    .AddOption(mentionableOption)
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
                var mentionable = (IMentionable) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_MENTIONABLE)?.Value;

                var commodity = this._objectRepo.GetById<Commodity>(targetCommodityId);
                var guild = ((SocketTextChannel)slashCommand.Channel).Guild;
                var guildState = this._objectRepo.GetById<GuildState>(guild.Id);

                var claims = this._objectRepo.GetWhere<Model.Claim>(c => c.CommodityId == targetCommodityId);
                foreach (var claim in claims)
                    this._objectRepo.Delete(claim);

                if (commodity.Deleted)
                {
                    this._objectRepo.Delete(commodity);
                    this._mediaRepo.Delete(commodity.ImageKey);

                    _ = this._commandBroker.RegisterAllAsync(guild);
                }

                var embed = new EmbedBuilder()
                                .WithDescription($"The **{commodity.Name}** {guildState.CommoditySingularTerm} has been revoked from {mentionable}.")
                                .Build();
                return slashCommand.RespondAsync(embed: embed, ephemeral: true);
            }

        }

    }
}
