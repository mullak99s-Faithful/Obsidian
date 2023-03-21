using Obsidian.SDK.Models;
using System.Text.Json;
using Obsidian.SDK.Enums;
using ObsidianAPI.Static;

namespace ObsidianAPI.Logic
{
	public interface IPackLogic
	{
		public IEnumerable<Pack> GetPacks();
		public Pack? GetPack(Guid guid);
		public Pack? GetPack(string packName);
		public Task<bool> AddPack(string packName, string description, Guid textureMappingsId);
		public Task<bool> AddBranch(Guid guid, string branchName, MinecraftVersion version);
		public Task<bool> AddPackPng(Guid guid, IFormFile packPng);
		public Task<bool> EditPack(Guid id, string? packName, string? description, Guid? textureMappingsId);
	}

	public class PackLogic : IPackLogic
	{
		public PackLogic()
		{
			Globals.Init();
		}

		public IEnumerable<Pack> GetPacks()
			=> Globals.Packs!;

		public Pack? GetPack(Guid guid)
			=> Globals.Packs!.Find(x => x.Id == guid);

		public Pack? GetPack(string packName)
			=> Globals.Packs!.Find(x => x.Name == packName);

		public async Task<bool> AddPack(string packName, string description, Guid textureMappingsId)
		{
			if (Globals.Packs!.Find(x => x.Name == packName) != null) return false;
			if (Globals.TextureMappings!.All(x => x.Id != textureMappingsId)) return false;

			Globals.Packs.Add(new Pack(packName, description, textureMappingsId));
			await Globals.SavePacks();
			return true;
		}

		public async Task<bool> EditPack(Guid id, string? packName, string? description, Guid? textureMappingsId)
		{
			Pack? pack = GetPack(id);
			if (pack == null) return false;

			Globals.Packs!.Remove(pack);

			if (!string.IsNullOrWhiteSpace(packName))
				pack.Name = packName;
			if (!string.IsNullOrWhiteSpace(description))
				pack.Description = description;
			if (textureMappingsId != null && !string.IsNullOrWhiteSpace(textureMappingsId.ToString()))
				pack.TextureMappingsId = textureMappingsId.Value;

			Globals.Packs.Add(pack);
			await Globals.SavePacks();
			return true;
		}

		public async Task<bool> AddBranch(Guid guid, string branchName, MinecraftVersion version)
		{
			Pack? pack = GetPack(guid);
			return pack != null && await AddBranchToPack(pack, branchName, version);
		}

		public async Task<bool> AddPackPng(Guid guid, IFormFile packPng)
		{
			Pack? pack = GetPack(guid);

			if (pack == null)
				return false;

			string dest = Path.Combine(Globals.PacksRootPath, guid.ToString(), "pack.png");

			await Utils.CopyFile(packPng, dest);

			string packPngBranches = Path.Combine(Globals.PacksRootPath, guid.ToString());
			foreach (PackBranch branch in pack.Branches)
				await Utils.CopyFile(packPng, Path.Combine(packPngBranches, branch.Id.ToString(), "pack.png"));

			return true;
		}

		private async Task<bool> AddBranchToPack(Pack pack, string branchName, MinecraftVersion version)
		{
			if (pack.Branches.Any(x => x.Name == branchName)) return false;
			pack.Branches.Add(new PackBranch(branchName, version));
			await Globals.SavePack(pack);
			return true;
		}
	}
}
