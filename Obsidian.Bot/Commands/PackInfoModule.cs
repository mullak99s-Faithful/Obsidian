using Discord;
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
			string packNames = string.Join("\n", packs.Select(x => x.Name).OrderBy(x => x));

			EmbedBuilder? embedBuilder = new EmbedBuilder()
				.WithTitle("Packs")
				.WithDescription(packNames)
				.WithColor(Color.Green)
				.WithCurrentTimestamp();

			await ReplyAsync(embed: embedBuilder.Build());
		}

		[Command("getbranches")]
		[Summary("Gets a list of all branches for a pack")]
		public async Task GetPackBranches([Remainder][Summary("Pack name")] string packName)
		{
			string packNameLower = packName.ToLower();
			List<Pack> packs = (await _packController.GetAll()).ToList();
			Pack? pack = packs.FirstOrDefault(x => x.Name.ToLower() == packNameLower || x.Name.ToLower().Contains(packNameLower));

			if (pack == null)
			{
				EmbedBuilder? errorEmbedBuilder = new EmbedBuilder()
					.WithTitle("Branches")
					.WithDescription("Pack not found!")
					.WithColor(Color.Red)
					.WithCurrentTimestamp();

				await ReplyAsync(embed: errorEmbedBuilder.Build());
				return;
			}

			string packNames = string.Join("\n", pack.Branches.Select(x => x.Name).OrderByDescending(x => x));
			EmbedBuilder? embedBuilder = new EmbedBuilder()
				.WithTitle("Branches")
				.WithDescription(packNames)
				.WithColor(Color.Green)
				.WithCurrentTimestamp();

			await ReplyAsync(embed: embedBuilder.Build());
		}
	}
}
