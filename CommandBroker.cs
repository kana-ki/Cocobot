using Cocobot.SlashCommands;
using Cocobot.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cocobot
{
    internal interface ICommandBroker
    {
        void Add<Factory, Handler>(string key)
            where Factory : ICommandFactory
            where Handler : ICommandHandler;
        Task HandleAsync(SocketSlashCommand slashCommand);
        Task RegisterAllAsync(SocketGuild guild);
    }

    internal class CommandBroker : ICommandBroker
    {
        private readonly TypeMap<ICommandFactory> _factories;
        private readonly TypeMap<ICommandHandler> _handlers;

        private readonly ILogger _logger;

        public CommandBroker(IServiceProvider serviceProvider, ILogger logger)
        {
            this._logger = logger;
            this._factories = new(serviceProvider);
            this._handlers = new(serviceProvider);
        }

        public void Add<Factory, Handler>(string key)
            where Factory : ICommandFactory
            where Handler : ICommandHandler
        {
            this._factories.Add<Factory>(key);
            this._handlers.Add<Handler>(key);
        }

        public Task RegisterAllAsync(SocketGuild guild)
        {
            var commandProperties = new List<SlashCommandProperties>();
            foreach (var factory in this._factories)
            {
                var command = factory.GetSlashCommand(guild);
                _ = this._logger.LogAsync("Configure", $"Registering application command {command.Name}");
                commandProperties.Add(command);
            }
            return guild.BulkOverwriteApplicationCommandAsync(commandProperties.ToArray());
        }

        public Task HandleAsync(SocketSlashCommand slashCommand) =>
            this._handlers.Activate(slashCommand.CommandName).HandleAsync(slashCommand);

    }
}
