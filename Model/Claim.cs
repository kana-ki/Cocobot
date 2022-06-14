using Cocobot.Persistance;
using System;

namespace Cocobot.Model
{
    internal class Claim : IEntity
    {
        public ulong Id { get; set; } = (ulong) new Random().NextInt64();
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong CommodityId { get; set; }
        public DateTime Claimed { get; set; }
        public ClaimType Type { get; set; }

    }
}
