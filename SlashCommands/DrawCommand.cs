using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class Draw {

        public const string COMMAND_NAME = "draw";
        public const string OPTION_CLAIM_TIME = "claimtime";

        public class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null)
            {
                var privateOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_CLAIM_TIME)
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Number)
                    .WithDescription("How many minutes the drawn commodity can be claimed for.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Do a new roulette draw!")
                    .AddOption(privateOption)
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }

        }

        public class CommandHandler : ICommandHandler
        {

            private readonly IObjectRepository _objectRepo;
            private readonly IRouletteRunner _rouletteRunner;

            public CommandHandler(IObjectRepository objectRepo, IRouletteRunner rouletteRunner)
            {
                this._objectRepo = objectRepo;
                this._rouletteRunner = rouletteRunner;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guild = ((SocketTextChannel) slashCommand.Channel).Guild;
                var guildConfig = this._objectRepo.GetById<GuildState>(guild.Id);

                var claimTime = (double) (slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_CLAIM_TIME)?.Value ?? default(double));

                await this._rouletteRunner.DrawAsync(guildConfig, slashCommand.Channel);

                var embed = new EmbedBuilder()
                                .WithDescription("A draw has been started! 🥳")
                                .Build();

                await slashCommand.RespondAsync(embed: embed, ephemeral: true);
            }

        }

    }
}
