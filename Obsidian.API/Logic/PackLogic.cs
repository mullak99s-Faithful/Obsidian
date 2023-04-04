using Obsidian.API.Static;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;

namespace Obsidian.API.Logic
{
	public interface IPackLogic
	{
		public IEnumerable<Pack> GetPacks();
		public Pack? GetPack(Guid guid);
		public Pack? GetPack(string packName);
		public Task<bool> AddPack(string packName, string description, Guid textureMappingsId);
		public Task<bool> EditPack(Guid id, string? packName, string? description, Guid? textureMappingsId);
		public Task<bool> DeletePack(Guid id);
		public Task<bool> AddBranch(Guid guid, string branchName, MinecraftVersion version);
		public Task<bool> EditBranch(Guid branchGuid, string? branchName, MinecraftVersion? version);
		public Task<bool> DeleteBranch(Guid branchGuid);
		public Task<bool> AddPackPng(Guid guid, IFormFile packPng);
	}

	public class PackLogic : IPackLogic
	{
		public PackLogic()
			=> Globals.Init();

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

		public async Task<bool> DeletePack(Guid id)
		{
			Pack? pack = GetPack(id);
			if (pack == null) return false;

			Globals.Packs!.Remove(pack);
			await Globals.SavePacks();
			return true;
		}

		public async Task<bool> AddBranch(Guid guid, string branchName, MinecraftVersion version)
		{
			Pack? pack = GetPack(guid);
			return pack != null && await AddBranchToPack(pack, branchName, version);
		}

		public async Task<bool> EditBranch(Guid branchGuid, string? branchName, MinecraftVersion? version)
		{
			Pack? pack = Globals.Packs?.Find(x => x.Branches.Any(y => y.Id == branchGuid));

			PackBranch? branch = pack?.Branches.Find(x => x.Id == branchGuid);
			if (branch == null) return false;

			pack!.Branches.Remove(branch);

			if (!string.IsNullOrWhiteSpace(branchName))
				branch.Name = branchName;
			if (version != null)
				branch.Version = version.Value;

			pack.Branches.Add(branch);

			await Globals.SavePack(pack);
			return true;
		}

		public async Task<bool> DeleteBranch(Guid branchGuid)
		{
			Pack? pack = Globals.Packs?.Find(x => x.Branches.Any(y => y.Id == branchGuid));

			PackBranch? branch = pack?.Branches.Find(x => x.Id == branchGuid);
			if (branch == null) return false;

			pack!.Branches.Remove(branch);
			await Globals.SavePack(pack);
			return true;
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
