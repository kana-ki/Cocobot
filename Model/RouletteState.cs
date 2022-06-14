using Cocobot.Persistance;
using System;
using System.Collections.Generic;

namespace Cocobot.Model
{
    public class RouletteState
    {

        public ulong EnabledChannel { get; set; }
        public TimeSpan Frequency { get; set; }
        public TimeSpan ClaimWindow { get; set; }
        public ulong LatestCommodityId { get; set; }
        public DateTime LatestCommodityPostedAt { get; set; }
        public DateTime LatestCommodityAvailableUntil { get; set; }
        public DateTime NextCommodityAvailableAt { get; set; }
        public List<ulong> ClaimedBy { get; set; } = new();

        public RouletteState() { 
            this.Frequency = TimeSpan.FromHours(1);
            this.ClaimWindow = TimeSpan.FromHours(20);
        }

    }
}
