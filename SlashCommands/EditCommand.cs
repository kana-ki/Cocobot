using Cocobot.Model;
using Cocobot.Persistance;
using Cocobot.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{

    public static class Edit
    {
        public const string COMMAND_NAME = "edit";
        public const string OPTION_COMMODITY = "commodity";
        public const string OPTION_NAME = "name";
        public const string OPTION_DESCRIPTION = "description";
        public const string OPTION_RARITY = "rarity";
        public const string OPTION_IMAGE = "image";
        public const string OPTION_LIMITED = "limited";

        internal class CommandFactory : ICommandFactory
        {

            private readonly IObjectRepository _objectRepo;

            public CommandFactory(IObjectRepository objectRepo) =>
                this._objectRepo = objectRepo;

            public SlashCommandProperties GetSlashCommand(SocketGuild guildContext)
            {
                var commodities = new List<Commodity>().AsQueryable();
                if (guildContext != null)
                    commodities = this._objectRepo.GetAll<Commodity>()
                        .Where(c => c.GuildId == guildContext.Id);

                var commodityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_COMMODITY)
                    .WithRequired(true)
                    .WithDescription("The commodity to edit.")
                    .WithType(ApplicationCommandOptionType.String);

                foreach (var commodity in commodities)
                {
                    var name = commodity.Name;
                    if (commodity.Deleted)
                        name += " (Deleted)";
                    commodityOption.AddChoice(name, commodity.Id.ToString());
                }

                var nameOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_NAME)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithDescription("The name of the commodity");

                var descriptionOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_DESCRIPTION)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithDescription("The description/lore for the commodity");

                var rarityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_RARITY)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("How rare of a commodity is this?")
                    .AddChoice("Common", 0)
                    .AddChoice("Uncommon", 1)
                    .AddChoice("Rare", 2)
                    .AddChoice("Epic", 3)
                    .AddChoice("Legendary", 4);

                var imageOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_IMAGE)
                    .WithType(ApplicationCommandOptionType.Attachment)
                    .WithDescription("The name of the commodity");

                var reservedOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_LIMITED)
                    .WithDescription("Withhold this commodity from the roulette, it'll only be awardable via /award.")
                    .WithType(ApplicationCommandOptionType.Boolean);

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Edit a commodity in the roulette!")
                    .AddOption(commodityOption)
                    .AddOption(nameOption)
                    .AddOption(descriptionOption)
                    .AddOption(rarityOption)
                    .AddOption(reservedOption)
                    .AddOption(imageOption)
                    .WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers)
                    .Build();
            }
        }

        internal class CommandHandler : ICommandHandler
        {
            private readonly HttpClient _httpClient;
            private readonly IObjectRepository _objectRepo;
            private readonly IMediaRepository _mediaRepo;
            private readonly ICommandBroker _commandBroker;

            public CommandHandler(HttpClient client, 
                                  IObjectRepository objectRepo,
                                  IMediaRepository mediaRepo,
                                  ICommandBroker commandProvider)

            {
                this._httpClient = client;
                this._objectRepo = objectRepo;
                this._mediaRepo = mediaRepo;
                this._commandBroker = commandProvider;
            }

            public async Task HandleAsync(SocketSlashCommand slashCommand)
            {
                var guild = ((SocketTextChannel)slashCommand.Channel).Guild;
                var targetCommodityId = slashCommand.GetUlongArg(OPTION_COMMODITY);
                var name = slashCommand.GetStringArg(OPTION_NAME);
                var description = slashCommand.GetStringArg(OPTION_DESCRIPTION);
                var rarity = slashCommand.GetEnumArg<Rarity>(OPTION_RARITY);
                var limited = slashCommand.GetBoolArg(OPTION_LIMITED);
                var image = slashCommand.GetObjectArg<Attachment>(OPTION_IMAGE);


                var commodity = this._objectRepo.GetById<Commodity>(targetCommodityId.Value);
                commodity.Name = name ?? commodity.Name;
                commodity.Description = description ?? commodity.Description;
                commodity.Rarity = rarity ?? commodity.Rarity;
                commodity.Limited = limited ?? commodity.Limited;
                this._objectRepo.Upsert(commodity);
                _ = this._commandBroker.RegisterAllAsync(guild);

                if (image != null)
                    _ = this._httpClient.GetStreamAsync(image.Url)
                        .ContinueWith(t => this._mediaRepo.Upload(commodity.ImageKey, image.ContentType, t.Result, CancellationToken.None));

                var guildConfig = this._objectRepo.GetById<GuildState>(guild.Id);
                var embeds = new Embed[] {
                    new EmbedBuilder().WithDescription($"Great! I've  update the {guildConfig?.CommoditySingularTerm}.").Build(),
                };
                await slashCommand.RespondAsync(embeds: embeds, ephemeral: commodity.Limited);
            }

        }

    }
}
