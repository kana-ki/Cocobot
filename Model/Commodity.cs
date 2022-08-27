using Cocobot.Persistance;
using Discord;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Cocobot.Model
{
    internal class Commodity : IEntity
    {

        public ulong Id { get; set; } = (ulong)new Random().NextInt64();
        public string Name { get; set; }
        public string Description { get; set; }
        public Rarity Rarity { get; set; }
        public string ImageKey { get; set; }
        public bool Limited { get; set; } 
        public ulong GuildId { get; set; }
        public bool Deleted { get; internal set; }

        public string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Rarity switch
            {
                Rarity.Common => ":large_blue_diamond:",
                Rarity.Uncommon => ":large_orange_diamond:",
                Rarity.Rare => ":diamond_shape_with_a_dot_inside:",
                Rarity.Epic => ":fleur_de_lis:",
                Rarity.Legendary => ":trident:",
            });
            stringBuilder.Append(" **");
            stringBuilder.Append(this.Name);
            stringBuilder.Append("** (");
            stringBuilder.Append(this.Rarity);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }

        public async Task<EmbedBuilder> ToEmbed(IMediaRepository mediaRepository)
        {
            var imageUri = await mediaRepository?.GetUri(this.ImageKey);
            return this.ToEmbed(imageUri.ToString());
        }

        public EmbedBuilder ToEmbed(string imageUri)
        {
            var stringBuilder = new StringBuilder();

            if (this.Description != null)
            {
                stringBuilder.AppendLine(this.Description);
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(this.Rarity switch
            {
                Rarity.Common => ":large_blue_diamond: Common",
                Rarity.Uncommon => ":large_orange_diamond: Uncommon",
                Rarity.Rare => ":diamond_shape_with_a_dot_inside: Rare",
                Rarity.Epic => ":fleur_de_lis: Epic",
                Rarity.Legendary => ":trident: Legendary",
            });

            var color = this.Rarity switch
            {
                Rarity.Common => Color.DarkBlue,
                Rarity.Uncommon => Color.Orange,
                Rarity.Rare => Color.Teal,
                Rarity.Epic => Color.Purple,
                Rarity.Legendary => Color.Gold,
            };

            if (this.Limited)
                stringBuilder.AppendLine("\n:secret: Limited edition. Available by award only.");

            return new EmbedBuilder()
                .WithColor(color)
                .WithTitle(this.Name)
                .WithDescription(stringBuilder.ToString())
                .WithImageUrl(imageUri);
        }
    }
}
