﻿using Cocobot.Model;
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
        Task DrawAsync(GuildState state, bool reset, IMessageChannel channel = null);
        void Start();
        void Stop();
    }

    internal class RouletteRunner : IDisposable, IRouletteRunner
    {
        private Timer _timer;
        private bool disposedValue;
        private readonly IObjectRepository _objectRepo;
        private readonly IMediaRepository _mediaRepo;
        private readonly IDiscordHandler _discordHandler;

        public RouletteRunner(IObjectRepository _repo, IDiscordHandler discordHander, IMediaRepository _mediaRepo)
        {
            this._timer = new Timer(this.OnTick, null, Timeout.Infinite, Timeout.Infinite);
            this._objectRepo = _repo;
            this._discordHandler = discordHander;
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

                Task.Factory.StartNew(() => this.DrawAsync(state, true), TaskCreationOptions.PreferFairness);
            }
        }

        public async Task DrawAsync(GuildState state, bool reset, IMessageChannel channel = null)
        {
            var commodities = this._objectRepo.GetWhere<Commodity>(c => c.GuildId == state.Id);
            var noOfCommodities = commodities.Count();
            if (noOfCommodities == 0)
                return;

            if (channel == null)
                channel = this._discordHandler.Client.GetChannel(state.RouletteState.EnabledChannel) as IMessageChannel;
            if (channel == null)
                return;

            var commodity = SelectRandom(commodities);

            state.RouletteState.LatestCommodityId = commodity.Id;
            state.RouletteState.LatestCommodityPostedAt = DateTime.UtcNow;
            state.RouletteState.LatestCommodityAvailableUntil = DateTime.UtcNow + state.RouletteState.ClaimWindow;
            if (reset)
                state.RouletteState.NextCommodityAvailableAt = DateTime.UtcNow + state.RouletteState.Frequency;
            state.RouletteState.ClaimedBy.Clear();
            this._objectRepo.Upsert(state);

            var embeds = new[]
            {
                new EmbedBuilder().WithDescription($"💕 💕 💕 💕 💕 💕 💕 💕 💕\nA new {state.CommoditySingularTerm} has appeared!\n" +
                    $"Quickly, **/claim** it!").Build(),
                (await commodity.ToEmbed(this._mediaRepo)).Build()
            };
            await channel.SendMessageAsync(state.RouletteState.DrawRoleMention > 0 ? MentionUtils.MentionRole(state.RouletteState.DrawRoleMention) : "", embeds: embeds);
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
