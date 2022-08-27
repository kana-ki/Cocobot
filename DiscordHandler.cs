using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Cocobot.Persistance;
using Cocobot.Model;

namespace Cocobot
{
    public interface IDiscordHandler
    {
        Task ListenAsync();
        DiscordSocketClient Client { get; }
    }

    internal class DiscordHandler : IDiscordHandler
    {

        public DiscordSocketClient Client { get; }

        private const string DISCORD_BOT_CONFIG_KEY = "DiscordBotToken";
        private readonly ICommandBroker _commandBroker;
        private readonly IComponentBroker _componentBroker;
        private readonly IObjectRepository _objectRepo;

        public DiscordHandler(IConfiguration config, ICommandBroker commandProvider, IComponentBroker componentBroker, IObjectRepository objectRepo)
        {
            this._commandBroker = commandProvider;
            this._componentBroker = componentBroker;
            this._objectRepo = objectRepo;
            var discordKey = config.GetValue<string>(DISCORD_BOT_CONFIG_KEY);
            if (discordKey == null)
                throw new Exception("Discord Bot Token not set!");

            this.Client = this.GetClient(discordKey);
            this.Client.GuildAvailable += GuildAvailableAsync;
            this.Client.SlashCommandExecuted += SlashCommandExecutedAsync;
            this.Client.SelectMenuExecuted += MessageComponentExecutedAsync;
            this.Client.ButtonExecuted += MessageComponentExecutedAsync;
        }

        public Task ListenAsync() => 
            this.Client.StartAsync();

        private Task GuildAvailableAsync(SocketGuild guild)
        {
            var existingGuildState = this._objectRepo.GetById<GuildState>(guild.Id);
            if (existingGuildState == null)
                this._objectRepo.Upsert(new GuildState(guild.Id));
            return this._commandBroker.RegisterAllAsync(guild);
        }

        private Task SlashCommandExecutedAsync(SocketSlashCommand slashCommand) =>
            this._commandBroker.HandleAsync(slashCommand);


        private Task MessageComponentExecutedAsync(SocketMessageComponent component) =>
            this._componentBroker.HandleAsync(component);

        private DiscordSocketClient GetClient(string discordBotToken)
        {
            var client = new DiscordSocketClient(new ()
            {
                LogLevel = LogSeverity.Verbose
            });
            client.Log += LogAsync;
            client.LoginAsync(TokenType.Bot, discordBotToken);
            return client;
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

    }
}
