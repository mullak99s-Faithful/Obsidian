using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace Obsidian.Bot.Services
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly LoggingService _loggingService;
		private readonly IServiceProvider _services;
		private string _commandPrefix = "!";

		// Retrieve client and CommandService instance via ctor
		public CommandHandler(DiscordSocketClient client, CommandService commands, LoggingService logging, IServiceProvider services)
		{
			_commands = commands;
			_client = client;
			_loggingService = logging;
			_services = services;
		}

		public void SetCommandPrefix(string prefix)
		{
			_commandPrefix = prefix;
		}

		public async Task InstallCommandsAsync()
		{
			// Client
			_client.MessageReceived += HandleCommandAsync;

			// Commands
			_commands.CommandExecuted += HandleCommandExecuted;

			// Auto-discover modules
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _services);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			// Don't process the command if it was a system message
			if (messageParam is not SocketUserMessage message) return;

			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;

			// Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (!(message.HasStringPrefix(_commandPrefix, ref argPos) ||
				message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
				message.Author.IsBot)
				return;

			// Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(_client, message);

			// Execute the command with the command context we just
			// created, along with the service provider for precondition checks.
			await _commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: _services);
		}

		private async Task HandleCommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			// Check if the command encountered an error
			if (!result.IsSuccess)
			{
				string? errorMessage = result.ErrorReason;
				_loggingService.Log($"Command error: {errorMessage}", LogSeverity.Error);

				if (command.IsSpecified)
					await context.Channel.SendMessageAsync($"Failed to execute command '{command.Value.Name}': {errorMessage}");
				else
					await context.Channel.SendMessageAsync($"An error occurred: {errorMessage}");
			}
		}
	}
}
