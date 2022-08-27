using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Award {

        public const string COMMAND_NAME = "award";
        public const string OPTION_USER = "user";
        public const string OPTION_COMMODITY = "commodity";

        public class CommandFactory : ICommandFactory
        {

            private readonly IObjectRepository _objectRepo;

            public CommandFactory(IObjectRepository objectRepo) =>
                this._objectRepo = objectRepo;


            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext)
            {
                IQueryable<Commodity> commodities = new List<Commodity>().AsQueryable();
                if (guildContext != null) 
                    commodities = this._objectRepo.GetAll<Commodity>()
                        .Where(c => c.GuildId == guildContext.Id);

                var userOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_USER)
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User)
                    .WithDescription("The user to award the commodity to.");

                var commodityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_COMMODITY)
                    .WithRequired(true)
                    .WithDescription("The commodity to award the user.")
                    .WithType(ApplicationCommandOptionType.String);

                foreach (var commodity in commodities)
                {
                    var name = commodity.Name;
                    if (commodity.Deleted)
                        name += " (Deleted)";
                    commodityOption.AddChoice(name, commodity.Id.ToString());
                }
                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Award a specific commodity to a user.")
                    .AddOption(userOption)
                    .AddOption(commodityOption)
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }

        }

        public class CommandHandler : ICommandHandler
        {

            private readonly IObjectRepository _objectRepo;
            private readonly IMediaRepository _mediaRepo;

            public CommandHandler(IObjectRepository objectRepo, IMediaRepository mediaRepo)
            {
                this._objectRepo = objectRepo;
                this._mediaRepo = mediaRepo;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guild = ((SocketTextChannel)slashCommand.Channel).Guild;
                var guildConfig = this._objectRepo.GetById<GuildState>(guild.Id);

                var targetCommodityId = ulong.Parse((string)slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_COMMODITY)?.Value);
                var targetUser = (SocketGuildUser)slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_USER)?.Value;

                var commodity = this._objectRepo.GetById<Commodity>(targetCommodityId);

                this._objectRepo.Upsert(new Model.Claim
                {
                    Type = ClaimType.Award,
                    CommodityId = commodity.Id,
                    GuildId = guild.Id,
                    Claimed = DateTime.UtcNow,
                    UserId = targetUser.Id
                });

                var embeds = new Embed[] {
                    new EmbedBuilder().WithDescription($"Ayo! {MentionUtils.MentionUser(targetUser.Id)} was awarded the **{commodity.Name}** {guildConfig?.CommoditySingularTerm ?? "commodity"}!").Build(),
                    (await commodity.ToEmbed(this._mediaRepo)).Build()
                };
                await slashCommand.RespondAsync(embeds: embeds);

            }

        }

    }
}
