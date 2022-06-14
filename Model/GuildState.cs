using Cocobot.Persistance;
using System;

namespace Cocobot.Model
{
    public class GuildState : IEntity
    {

        public ulong Id { get; set; }
        public string CommoditySingularTerm { get; set; }
        public string CommodityPluralTerm { get; set; }

        public RouletteState RouletteState { get; set; }

        public GuildState() {  }

        public GuildState(ulong guildId)
        {
            this.Id = guildId;
            this.CommodityPluralTerm = "commodities";
            this.CommoditySingularTerm = "commodity";
            this.RouletteState = new RouletteState();
        }

    }
}
