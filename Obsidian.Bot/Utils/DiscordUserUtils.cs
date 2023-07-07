using Discord.WebSocket;

namespace Obsidian.Bot.Utils
{
	public static class DiscordUserUtils
	{
		public static string GetUsername(this SocketUser user)
			=> user.Discriminator != "0000" ? $"{user.Username}#{user.Discriminator}" : $"{user.Username}";
	}
}
