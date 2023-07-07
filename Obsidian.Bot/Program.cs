using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Obsidian.Bot.Services;
using Obsidian.SDK;
using Obsidian.SDK.Controllers;
using Obsidian.SDK.Models.Auth;

namespace Obsidian.Bot
{
	public class Program
	{
		private readonly DiscordSocketClient _client;

		private readonly CommandService _commands;
		private readonly IServiceProvider _services;
		private readonly LoggingService _loggingService;
		private readonly CommandHandler _commandHandler;

		public static IConfiguration Configuration { get; set; }

		private Program()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig
			{
				LogLevel = LogSeverity.Info,
				MessageCacheSize = 100,
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
			});

			_commands = new CommandService(new CommandServiceConfig
			{
				LogLevel = LogSeverity.Info,
				CaseSensitiveCommands = false
			});

			IHost host = CreateHostBuilder().Build();
			Configuration = (IConfiguration)host.Services.GetService(typeof(IConfiguration))! ?? throw new InvalidOperationException();

			_loggingService = new LoggingService(_client, _commands, Configuration);

			_services = ConfigureServices();
			_commandHandler = new CommandHandler(_client, _commands, _loggingService, _services);
		}

		public static Task Main(string[] args) => new Program().MainAsync(args);

		public async Task MainAsync(string[] args)
		{
			_commandHandler.SetCommandPrefix(Configuration["Discord:Prefix"] ?? "!");
			await _commandHandler.InstallCommandsAsync();

			await _client.LoginAsync(TokenType.Bot, Configuration["Discord:Token"] ?? string.Empty);
			await _client.StartAsync();

			await Task.Delay(Timeout.Infinite);
		}

		private static IServiceProvider ConfigureServices()
		{
			var services = new ServiceCollection();

			// Services
			var httpClient = new HttpClient();
			services.AddSingleton(httpClient);
			services.AddSingleton(_ =>
			{
				string endpoint = Configuration["Obsidian.API:Endpoint"] ?? string.Empty;
				return new ApiClient(httpClient, endpoint, new LoginModel()
				{
					Username = Configuration["Obsidian.API:Login:Username"] ?? string.Empty,
					Password = Configuration["Obsidian.API:Login:Password"] ?? string.Empty
				});
			});

			services.AddSingleton<PackController>();

			return services.BuildServiceProvider(validateScopes: true);
		}

		static IHostBuilder CreateHostBuilder() =>
			Host.CreateDefaultBuilder();
	}
}