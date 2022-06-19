using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Cocobot;
using Cocobot.Utils;
using Cocobot.SlashCommands;
using Cocobot.Persistance;
using System.Net.Http;

var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables("COCOBOT_")
                    .Build();

var connectionString = config.GetValue<string>("Persistence:ConnectionString");

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IConfiguration>(config);
serviceCollection.AddSingleton<IObjectRepository>(new LiteDbRepository(connectionString)); 
serviceCollection.AddSingleton<IMediaRepository, AzureMediaRepository>(); 
serviceCollection.AddSingleton<ILogger, Logger>();
serviceCollection.AddSingleton(new HttpClient());
serviceCollection.AddSingleton<IDiscordHandler, DiscordHandler>();
serviceCollection.AddSingleton<ICommandBroker, CommandBroker>();
serviceCollection.AddSingleton<IComponentBroker, ComponentBroker>();
serviceCollection.AddSingleton<IRouletteRunner, RouletteRunner>();
var serviceProvider = serviceCollection.BuildServiceProvider();

var commandBroker = serviceProvider.GetService<ICommandBroker>();
commandBroker.Add<Claim.CommandFactory, Claim.CommandHandler>(Claim.COMMAND_NAME);
commandBroker.Add<Enable.CommandFactory, Enable.CommandHandler>(Enable.COMMAND_NAME);
commandBroker.Add<CreateCommand.CommandFactory, CreateCommand.CommandHandler>(CreateCommand.COMMAND_NAME);
commandBroker.Add<Award.CommandFactory, Award.CommandHandler>(Award.COMMAND_NAME);
commandBroker.Add<List.CommandFactory, List.CommandHandler>(List.COMMAND_NAME);
commandBroker.Add<CommodityTerm.CommandFactory, CommodityTerm.CommandHandler>(CommodityTerm.COMMAND_NAME);
commandBroker.Add<Deck.CommandFactory, Deck.CommandHandler>(Deck.COMMAND_NAME);
commandBroker.Add<Revoke.CommandFactory, Revoke.CommandHandler>(Revoke.COMMAND_NAME);
commandBroker.Add<Delete.CommandFactory, Delete.CommandHandler>(Delete.COMMAND_NAME);
commandBroker.Add<Frequency.CommandFactory, Frequency.CommandHandler>(Frequency.COMMAND_NAME);
commandBroker.Add<ClaimTime.CommandFactory, ClaimTime.CommandHandler>(ClaimTime.COMMAND_NAME);
commandBroker.Add<ShowMy.CommandFactory, ShowMy.CommandHandler>(ShowMy.COMMAND_NAME);
commandBroker.Add<Draw.CommandFactory, Draw.CommandHandler>(Draw.COMMAND_NAME);
commandBroker.Add<Info.CommandFactory, Info.CommandHandler>(Info.COMMAND_NAME);
commandBroker.Add<Edit.CommandFactory, Edit.CommandHandler>(Edit.COMMAND_NAME);

var componentBroker = serviceProvider.GetService<IComponentBroker>();
componentBroker.Add<ShowMy.ComponentHandler>(ShowMy.COMPONENT_NAME);

await serviceProvider.GetService<IDiscordHandler>().ListenAsync();
serviceProvider.GetService<IRouletteRunner>().Start();

await Task.Delay(Timeout.Infinite);
 