using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class CommodityTerm
    {
        public const string COMMAND_NAME = "commodityterm";
        public const string OPTION_SINGULAR = "singularterm";
        public const string OPTION_PLURAL = "pluralterm";

        internal class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var singularTermOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_SINGULAR)
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithDescription("The term to use instead of 'commodity'.");

                var pluralTermOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_PLURAL)
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithDescription("The term to use instead of 'commodities'.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Change the word Cocobot uses instead of \"commodity\" and \"commodities\".")
                    .AddOption(singularTermOption)
                    .AddOption(pluralTermOption)
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

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;

                var singular = (string) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_SINGULAR)?.Value;
                var plural = (string) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_PLURAL)?.Value;

                var guildConfig = this._objectRepo.GetById<GuildState>(guildId);

                guildConfig.CommoditySingularTerm = singular;
                guildConfig.CommodityPluralTerm = plural;

                this._objectRepo.Upsert(guildConfig);

                await slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"I'll call them {plural} from now on. ♥").Build());
            }

        }


    }
}
