using Cocobot.Model;
using Cocobot.Persistance;
using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cocobot.SlashCommands
{

    public static class CreateCommand
    {
        public const string COMMAND_NAME = "create";
        public const string OPTION_NAME = "name";
        public const string OPTION_DESCRIPTION = "description";
        public const string OPTION_RARITY = "rarity";
        public const string OPTION_IMAGE = "image";
        public const string OPTION_LIMITED = "limited";

        internal class CommandFactory : ICommandFactory
        {

            public SlashCommandProperties GetSlashCommand(SocketGuild guild = null)
            {
                var nameOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_NAME)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true)
                    .WithDescription("The name of the commodity");

                var descriptionOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_DESCRIPTION)
                    .WithType(ApplicationCommandOptionType.String)
                    .WithDescription("The description/lore for the commodity");

                var rarityOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_RARITY)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("How rare of a commodity is this")
                    .AddChoice("Common", 0)
                    .AddChoice("Uncommon", 1)
                    .AddChoice("Rare", 2)
                    .AddChoice("Epic", 3)
                    .AddChoice("Legendary", 4);

                var imageOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_IMAGE)
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Attachment)
                    .WithDescription("The name of the commodity");

                var reservedOption = new SlashCommandOptionBuilder()
                    .WithName(OPTION_LIMITED)
                    .WithDescription("Withhold this commodity from the roulette, it'll only be awardable via /award.")
                    .WithType(ApplicationCommandOptionType.Boolean);

                return new SlashCommandBuilder()
                    .WithName(COMMAND_NAME)
                    .WithDescription("Create a new commodity for the roulette!")
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
                var name = (string) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_NAME)?.Value;
                var description = (string) slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_DESCRIPTION)?.Value;
                var rarity = (Rarity)Convert.ToInt32(slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_RARITY)?.Value);
                var limited = (bool) (slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_LIMITED)?.Value ?? false);
                var image = (Attachment)slashCommand.Data.Options.FirstOrDefault(o => o.Name == OPTION_IMAGE)?.Value;

                var guildConfig = this._objectRepo.GetById<GuildState>(guild.Id);

                var commodity = new Commodity()
                {
                    Name = name,
                    Description = description,
                    Rarity = rarity,
                    Limited = limited,
                    GuildId = guild.Id
                };
                commodity.ImageKey = commodity.Id.ToString();
                this._objectRepo.Upsert(commodity);
                _ = this._commandBroker.RegisterAllAsync(guild);


                _ = await this._httpClient.GetStreamAsync(image.Url)
                    .ContinueWith(t => this._mediaRepo.Upload(commodity.ImageKey, image.ContentType, t.Result, CancellationToken.None));

                var embeds = new Embed[] {
                    new EmbedBuilder().WithDescription($"Wooo! New {guildConfig?.CommoditySingularTerm} added.").Build(),
                    commodity.ToEmbed(image.Url).Build()
                };
                await slashCommand.RespondAsync(embeds: embeds, ephemeral: limited);
            }

        }

    }
}
