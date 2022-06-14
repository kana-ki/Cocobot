using Cocobot.SlashCommands;
using Cocobot.Utils;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Cocobot
{
    internal interface IComponentBroker
    {
        void Add<Handler>(string key) where Handler : IComponentHandler;
        Task HandleAsync(SocketMessageComponent component);
    }

    internal class ComponentBroker : IComponentBroker
    {
        private readonly TypeMap<IComponentHandler> _handlers;

        public ComponentBroker(IServiceProvider serviceProvider)
        {
            this._handlers = new(serviceProvider);
        }

        public void Add<Handler>(string key) where Handler : IComponentHandler
        {
            this._handlers.Add<Handler>(key);
        }

        public Task HandleAsync(SocketMessageComponent component) =>
            this._handlers.Activate(component.Data.CustomId).HandleAsync(component);

    }
}
