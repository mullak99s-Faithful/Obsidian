using Discord.Commands;
using Discord.WebSocket;
using Obsidian.Bot.Utils;

namespace Obsidian.Bot.Commands
{
	public class InfoModule : ModuleBase<SocketCommandContext>
	{
		[Command("say")]
		[Summary("Echoes a message.")]
		public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
			=> ReplyAsync(echo);

		[Command("userinfo")]
		[Summary("Returns info about the current user, or the user parameter, if one passed.")]
		[Alias("user", "whois")]
		public async Task UserInfoAsync([Summary("The (optional) user to get info from")] SocketUser? user = null)
		{
			var userInfo = user ?? Context.Client.CurrentUser;

			if (userInfo.Id == Context.Client.CurrentUser.Id)
				await ReplyAsync($"You want to know about me? I'm {userInfo.GetUsername()}!");
			else if (userInfo.Id == Context.User.Id)
				await ReplyAsync($"You want to know about yourself? You're {userInfo.GetUsername()}!");
			else
				await ReplyAsync($"{userInfo.GetUsername()}");
		}

		[Command("error")]
		[Summary("Throws an exception")]
		[Alias("exception")]
		public Task ThrowException([Summary("Exception message")] string? message = null)
		{
			throw new Exception(message ?? "Test");
		}
	}
}
