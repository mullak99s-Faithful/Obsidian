using Discord.Commands;
using Obsidian.SDK.Controllers;
using Obsidian.SDK.Models;

namespace Obsidian.Bot.Commands
{
	public class PackInfoModule : ModuleBase<SocketCommandContext>
	{
		private readonly PackController _packController;

		public PackInfoModule(PackController packController)
		{
			_packController = packController;
		}

		[Command("getpacknames")]
		[Summary("Gets a list of all resource pack names")]
		public async Task GetPackNamesAsync()
		{
			List<Pack> packs = (await _packController.GetAll()).ToList();
			await ReplyAsync(string.Join(", ", packs.Select(x => x.Name)));
		}
	}
}
