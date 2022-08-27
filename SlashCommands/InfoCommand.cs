using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using moment.net;
using PrettyPrintNet;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Info
    {

        public const string COMMAND_NAME = "info";

        public class CommandFactory : ICommandFactory
        {
            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Show current roulette information and settings.")
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }
        }

        public class CommandHandler : ICommandHandler
        {

            private readonly IObjectRepository _objectRepo;
            private readonly IDiscordHandler _discordHandler;

            public CommandHandler(IObjectRepository objectRepo, IDiscordHandler discordHandler)
            {
                this._objectRepo = objectRepo;
                this._discordHandler = discordHandler;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guild = ((SocketTextChannel)slashCommand.Channel).Guild;
                var guildState = this._objectRepo.GetById<GuildState>(guild.Id);
                var commodities = this._objectRepo.GetWhere<Commodity>(c => c.GuildId == guild.Id);


                var rouletteStateStringBuilder = new StringBuilder();

                if (guildState.RouletteState.EnabledChannel != 0)
                {
                    var channel = this._discordHandler.Client.GetChannel(guildState.RouletteState.EnabledChannel) as IMessageChannel;
                    rouletteStateStringBuilder.Append("**Enabled**: Enabled on #");
                    rouletteStateStringBuilder.AppendLine(channel.Name);
                }
                else
                {
                    rouletteStateStringBuilder.AppendLine("**Enabled**: Not Enabled");
                }
                rouletteStateStringBuilder.Append("**Previous draw**: ");
                rouletteStateStringBuilder.AppendLine(guildState.RouletteState.LatestCommodityPostedAt.FromNow());
                rouletteStateStringBuilder.Append("**Number claimed**: ");
                rouletteStateStringBuilder.AppendLine(guildState.RouletteState.ClaimedBy.Count.ToString());
                rouletteStateStringBuilder.Append("**Claimable until**: ");
                if (guildState.RouletteState.LatestCommodityAvailableUntil.IsAfter(System.DateTime.UtcNow))
                    rouletteStateStringBuilder.AppendLine(guildState.RouletteState.LatestCommodityAvailableUntil.ToNow());
                else
                    rouletteStateStringBuilder.AppendLine(guildState.RouletteState.LatestCommodityAvailableUntil.FromNow());
                if (guildState.RouletteState.EnabledChannel != 0) {
                    rouletteStateStringBuilder.Append("**Next draw**: ");
                    rouletteStateStringBuilder.Append(guildState.RouletteState.NextCommodityAvailableAt.ToNow());
                }


                var rarities = commodities.GroupBy(c => c.Rarity).OrderBy(c => c.Key);
                var totalWeight = 0;
                var rarityWeight = new float[] { 0, 0, 0, 0, 0 };
                foreach (var rarity in rarities)
                {
                    var weight = rarity.Count() * (rarity.Key switch
                    {
                        Rarity.Common => 9,
                        Rarity.Uncommon => 7,
                        Rarity.Rare => 5,
                        Rarity.Epic => 3,
                        Rarity.Legendary => 1,
                    });
                    rarityWeight[(int)rarity.Key] = weight;
                    totalWeight += weight;
                }
                var chancesStringBuilder = new StringBuilder();
                chancesStringBuilder.Append("**Chance of Common**: ");
                chancesStringBuilder.AppendLine($"{rarityWeight[0] / totalWeight * 100:0.00}%");
                chancesStringBuilder.Append("**Chance of Uncommon**: ");
                chancesStringBuilder.AppendLine($"{rarityWeight[1] / totalWeight * 100:0.00}%");
                chancesStringBuilder.Append("**Chance of Rare**: ");
                chancesStringBuilder.AppendLine($"{rarityWeight[2] / totalWeight * 100:0.00}%");
                chancesStringBuilder.Append("**Chance of Epic**: ");
                chancesStringBuilder.AppendLine($"{rarityWeight[3] / totalWeight * 100:0.00}%");
                chancesStringBuilder.Append("**Chance of Legendary**: ");
                chancesStringBuilder.Append($"{rarityWeight[4] / totalWeight * 100:0.00}%");


                var settingsStateStringBuilder = new StringBuilder();
                settingsStateStringBuilder.Append("**Singular commodity**: ");
                settingsStateStringBuilder.AppendLine(guildState.CommoditySingularTerm);
                settingsStateStringBuilder.Append("**Plural commodity**: ");
                settingsStateStringBuilder.AppendLine(guildState.CommodityPluralTerm);
                settingsStateStringBuilder.Append("**Roulette Frequency**: ");
                settingsStateStringBuilder.AppendLine(guildState.RouletteState.Frequency.ToPrettyString());
                settingsStateStringBuilder.Append("**Roulette Claim Limit**: ");
                settingsStateStringBuilder.AppendLine(guildState.RouletteState.ClaimLimit < 1 ? guildState.RouletteState.ClaimLimit.ToString() : "None");
                settingsStateStringBuilder.Append("**Roulette Claim Time**: ");
                settingsStateStringBuilder.Append(guildState.RouletteState.ClaimWindow.ToPrettyString());


                var embeds = new Embed[] {
                    new EmbedBuilder().WithDescription(rouletteStateStringBuilder.ToString()).Build(),
                    new EmbedBuilder().WithDescription(chancesStringBuilder.ToString()).Build(),
                    new EmbedBuilder().WithDescription(settingsStateStringBuilder.ToString()).Build(),
                };
                await slashCommand.RespondAsync(embeds: embeds, ephemeral: false);
            }

        }

    }
}
