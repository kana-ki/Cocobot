using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    public static class DrawRole
    {
        public const string COMMAND_NAME = "drawrole";
        public const string OPTION_ROLE = "role";

        internal class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var frequency = new SlashCommandOptionBuilder()
                    .WithName(OPTION_ROLE)
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Role)
                    .WithMinValue(0)
                    .WithDescription("The role to mention when a new draw starts.");

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Set a discord role to mention when a new draw starts.")
                    .AddOption(frequency)
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
                var guildId = ((SocketTextChannel)slashCommand.Channel).Guild.Id;
                var guildConfig = this._objectRepo.GetById<GuildState>(guildId);

                var role = slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_ROLE)?.Value as SocketRole;

                if (role != null)
                {
                    guildConfig.RouletteState.DrawRoleMention = role.Id;
                    _ = slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"I'll mention that role in future draws! ♥").Build(), ephemeral: false);
                }
                else
                {
                    guildConfig.RouletteState.DrawRoleMention = 0;
                    _ = slashCommand.RespondAsync(embed: new EmbedBuilder().WithDescription($"I won't mention any role in future draws. 🙂").Build(), ephemeral: false);
                }

                this._objectRepo.Upsert(guildConfig);
                return Task.CompletedTask;
            }

        }

    }
}
