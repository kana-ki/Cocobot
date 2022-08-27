using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Claim { 

        public const string COMMAND_NAME = "claim";

        internal class CommandFactory: ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null) =>
                new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Claim the currently available commodity and place it in your collection!")
                    .Build();

        } 

        internal class CommandHandler : ICommandHandler
        {

            private readonly IObjectRepository _objectRepo;

            public CommandHandler(IObjectRepository objectRepo) =>
                this._objectRepo = objectRepo;

            public Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;
                var userId = slashCommand.User.Id;

                var guildState = this._objectRepo.GetById<GuildState>(guildId);
                var commoditySingular = guildState?.CommoditySingularTerm ?? "commodity";

                if (guildState == null || guildState.RouletteState == null || guildState.RouletteState.LatestCommodityId == 0)
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"There are no {commoditySingular} available to claim right now! :disappointed:").Build(), ephemeral: true);

                if (guildState.RouletteState.ClaimedBy.Contains(userId))
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"You've already claimed this {commoditySingular}. :star_struck:").Build(), ephemeral: true);

                if (guildState.RouletteState.ClaimLimit > 0 && guildState.RouletteState.ClaimedBy.Count >= guildState.RouletteState.ClaimLimit)
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"Sorry, you missed this {commoditySingular}; they've all been claimed! Good luck in the next drop! ♥").Build(), ephemeral: true);

                if (DateTime.UtcNow > guildState.RouletteState.LatestCommodityAvailableUntil)
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"Sorry, you missed this {commoditySingular}; time is up! Good luck in the next drop! ♥").Build(), ephemeral: true);

                var commodity = this._objectRepo.GetById<Commodity>(guildState.RouletteState.LatestCommodityId);

                if (commodity == null || commodity.Deleted)
                    return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"That {commoditySingular} no longer exists. :shrug:").Build(), ephemeral: true);

                this._objectRepo.Upsert(new Model.Claim
                {
                    Type = ClaimType.Claim,
                    CommodityId = guildState.RouletteState.LatestCommodityId,
                    GuildId = guildId,
                    Claimed = DateTime.UtcNow,
                    UserId = userId
                });

                guildState.RouletteState.ClaimedBy.Add(userId);
                this._objectRepo.Upsert(guildState);

                return slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"Yay! {MentionUtils.MentionUser(userId)} claimed the **{commodity.Name}** {guildState?.CommoditySingularTerm ?? "commodity"}!").Build());
            }

        }
   
    }
}
