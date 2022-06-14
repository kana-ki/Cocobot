using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{
    internal interface ICommandFactory
    {
        SlashCommandProperties GetSlashCommand(SocketGuild guildContext = null);
    }

    internal interface ICommandHandler
    {
        Task HandleAsync(SocketSlashCommand slashCommand);
    }

    internal interface IComponentHandler
    {
        Task HandleAsync(SocketMessageComponent component);
    }

}
