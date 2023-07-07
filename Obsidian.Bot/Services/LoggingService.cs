using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Obsidian.Bot.Services
{
	public class LoggingService
	{
		private readonly IConfiguration _configuration;

		public LoggingService(DiscordSocketClient client, CommandService command, IConfiguration configuration)
		{
			_configuration = configuration;
			client.Log += Log;
			command.Log += Log;
		}

		public void Log(string message, LogSeverity severity = LogSeverity.Info)
		{
			SetConsoleColor(severity);
			Console.WriteLine($"{DateTime.Now,-19} [{severity,8}] {_configuration["Bot:Name"]}: {message}");
			Console.ResetColor();
		}

		public void Log(string message, Exception exception, LogSeverity severity = LogSeverity.Error)
		{
			SetConsoleColor(severity);
			Console.WriteLine($"{DateTime.Now,-19} [{severity,8}] {_configuration["Bot:Name"]}: {message} {exception}");
			Console.ResetColor();
		}

		private static Task Log(LogMessage message)
		{
			SetConsoleColor(message.Severity);
			Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
			Console.ResetColor();

			return Task.CompletedTask;
		}

		private static void SetConsoleColor(LogSeverity severity)
		{
			switch (severity)
			{
				case LogSeverity.Critical:
				case LogSeverity.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case LogSeverity.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				case LogSeverity.Info:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case LogSeverity.Verbose:
				case LogSeverity.Debug:
					Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
			}
		}
	}
}
