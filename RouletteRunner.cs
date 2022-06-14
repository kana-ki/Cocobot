using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Weighted_Randomizer;

namespace Cocobot
{
    public interface IRouletteRunner
    {
        void Dispose();
        void Enable(SocketTextChannel channel);
        void Disable(SocketTextChannel channel);
        Task DrawAsync(GuildState state, IMessageChannel channel);
        void Start();
        void Stop();
    }

    internal class RouletteRunner : IDisposable, IRouletteRunner
    {
        private Timer _timer;
        private bool disposedValue;
        private readonly IObjectRepository _objectRepo;
        private readonly IMediaRepository _mediaRepo;
        private readonly IDiscordHandler _discordHander;

        public RouletteRunner(IObjectRepository _repo, IDiscordHandler discordHander, IMediaRepository _mediaRepo)
        {
            this._timer = new Timer(this.OnTick, null, Timeout.Infinite, Timeout.Infinite);
            this._objectRepo = _repo;
            this._discordHander = discordHander;
            this._mediaRepo = _mediaRepo;
        }

        public void Start() =>
            this._timer.Change(0, 60_000);

        public void Stop() =>
            this._timer.Change(Timeout.Infinite, Timeout.Infinite);

        public void Enable(SocketTextChannel channel)
        {
            var guild = channel.Guild;
            var state = this._objectRepo.GetById<GuildState>(guild.Id) ?? new GuildState(guild.Id);
            state.RouletteState.EnabledChannel = channel.Id;
            this._objectRepo.Upsert(state);
        }

        public void Disable(SocketTextChannel channel)
        {
            var guild = channel.Guild;
            var state = this._objectRepo.GetById<GuildState>(guild.Id);
            if (state == null) return;
            state.RouletteState.EnabledChannel = 0;
            this._objectRepo.Upsert(state);
        }

        private void OnTick(object _)
        {
            var states = this._objectRepo.GetAll<GuildState>();
            foreach (var state in states)
            {
                if (state.RouletteState == null || state.RouletteState.EnabledChannel == 0)
                    continue;
                if (state.RouletteState.NextCommodityAvailableAt.ToUniversalTime() > DateTime.UtcNow)
                    continue;

                Task.Factory.StartNew(() => this.ScheduledDraw(state), TaskCreationOptions.PreferFairness);
            }
        }

        public async Task DrawAsync(GuildState state, IMessageChannel channel)
        {
            var commodities = this._objectRepo.GetWhere<Commodity>(c => c.GuildId == state.Id);
            var noOfCommodities = commodities.Count();
            if (noOfCommodities == 0)
                return;

            var commodity = SelectRandom(commodities);

            state.RouletteState.LatestCommodityId = commodity.Id;
            state.RouletteState.LatestCommodityPostedAt = DateTime.UtcNow;
            state.RouletteState.LatestCommodityAvailableUntil = DateTime.UtcNow + state.RouletteState.ClaimWindow;
            state.RouletteState.ClaimedBy.Clear();
            this._objectRepo.Upsert(state);

            var embeds = new[]
            {
                new EmbedBuilder().WithDescription($"A new {state.CommoditySingularTerm} has appeared! Quickly, claim it!").Build(),
                await commodity.ToEmbed(this._mediaRepo)
            };
            _ = channel.SendMessageAsync(embeds: embeds);
        }

        private async Task ScheduledDraw(GuildState state)
        {
            var commodities = this._objectRepo.GetWhere<Commodity>(c => c.GuildId == state.Id);
            var noOfCommodities = commodities.Count();
            if (noOfCommodities == 0)
                return;

            var channel = this._discordHander.Client.GetChannel(state.RouletteState.EnabledChannel) as IMessageChannel;
            if (channel == null)
                return;

            Commodity commodity = SelectRandom(commodities);
            state.RouletteState.LatestCommodityId = commodity.Id;
            state.RouletteState.LatestCommodityPostedAt = DateTime.UtcNow;
            state.RouletteState.LatestCommodityAvailableUntil = DateTime.UtcNow + state.RouletteState.ClaimWindow;
            state.RouletteState.NextCommodityAvailableAt = DateTime.UtcNow + state.RouletteState.Frequency;
            state.RouletteState.ClaimedBy.Clear();
            this._objectRepo.Upsert(state);

            var embeds = new[]
            {
                new EmbedBuilder().WithDescription($"A new {state.CommoditySingularTerm} has appeared! Quickly, claim it!").Build(),
                await commodity.ToEmbed(this._mediaRepo)
            };
            _ = channel.SendMessageAsync(embeds: embeds);
        }

        private static Commodity SelectRandom(IEnumerable<Commodity> commodities)
        {
            var randomizer = new StaticWeightedRandomizer<Commodity>();
            foreach (var commodity in commodities)
                randomizer.Add(commodity, commodity.Rarity switch
                {
                    Rarity.Common => 9,
                    Rarity.Uncommon => 7,
                    Rarity.Rare => 5,
                    Rarity.Epic => 3,
                    Rarity.Legendary => 1,
                });
            return randomizer.NextWithReplacement();
            
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._timer.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
