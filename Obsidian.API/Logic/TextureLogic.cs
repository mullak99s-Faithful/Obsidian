using System.IO.Compression;
using System.Text.Json;
using Obsidian.API.Static;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;

namespace Obsidian.API.Logic
{
	public interface ITextureLogic
	{
		public Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile);
		public bool ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite);
		public bool GeneratePacks(List<Guid> packIds);
		public List<Asset> SearchForTextures(Guid packId, string searchQuery);
	}

	public class TextureLogic : ITextureLogic
	{
		public TextureLogic()
		{
			Globals.Init();
			LoadTextures();
		}

		private void LoadTextures()
		{
			string[] jsons = Directory.GetFiles(Globals.MasterAssetsRootPath);
			Globals.MasterAssets = new List<MasterAsset>();
			foreach (string map in jsons)
			{
				if (File.Exists(map))
				{
					try
					{
						string rawJson = File.ReadAllText(map);
						TextureMapping? mapObj = JsonSerializer.Deserialize<TextureMapping>(rawJson);
						if (mapObj != null)
							Globals.TextureMappings?.Add(mapObj);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}
		}

		private async Task SaveTextureMaps()
		{
			List<Task> saveTasks = Globals.TextureMappings!.Select(SaveTextureMap).ToList();
			await Task.WhenAll(saveTasks);
		}

		private async Task SaveTextureMap(TextureMapping map)
		{
			string json = JsonSerializer.Serialize(map);
			string jsonPath = Path.Combine(Globals.TextureMappingsRootPath, $"{map.Id}.json");
			File.Delete(jsonPath);
			await File.WriteAllTextAsync(jsonPath, json);
		}

		public async Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile)
		{
			bool success = true;
			foreach (Guid packId in packIds)
			{
				Pack? pack = Globals.Packs!.Find(x => x.Id == packId);
				if (pack == null)
				{
					success = false;
					continue;
				}

				TextureMapping? textureMapping = Globals.TextureMappings!.Find(x => x.Id == pack.TextureMappingsId);
				if (textureMapping == null)
				{
					success = false;
					continue;
				}

				Asset? asset = textureMapping.Assets.Find(x => x.Names.Contains(textureName.ToUpper()));
				if (asset == null)
				{
					success = false;
					continue;
				}

				string path = Path.Combine(Globals.MasterAssetsRootPath, pack.Id.ToString(), "texture");
				Directory.CreateDirectory(path);

				string filePath = Path.Combine(path, $"{asset.Id}.png");
				await using var stream = textureFile.OpenReadStream();
				await using var fileStream = new FileStream(filePath, FileMode.Create);
				await stream.CopyToAsync(fileStream);

				if (mcMetaFile != null)
				{
					await using var metaStream = mcMetaFile.OpenReadStream();
					await using var metaFileStream = new FileStream($"{filePath}.mcmeta", FileMode.Create);
					await metaStream.CopyToAsync(metaFileStream);
				}

				string destinationPackPath = Path.Combine(Globals.PacksRootPath, pack.Id.ToString());
				foreach (PackBranch branch in pack.Branches)
				{
					string dest = Path.Combine(destinationPackPath, branch.Id.ToString());

					foreach (var t in asset.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
					{
						string fDest = Path.Combine(dest, t.Path);
						await Utils.CopyFile(textureFile, fDest);

						if (t.MCMeta && mcMetaFile != null)
						{
							string fDestMeta = Path.Combine(dest, t.MCMetaPath);
							await Utils.CopyFile(mcMetaFile, fDestMeta);
						}
					}
				}
			}
			return success;
		}

		public List<Asset> SearchForTextures(Guid packId, string searchQuery)
		{
			Pack? pack = Globals.Packs?.Find(x => x.Id == packId);

			if (pack == null)
				return new List<Asset>();

			TextureMapping? map = Globals.TextureMappings?.Find(x => x.Id == pack.TextureMappingsId);

			if (map == null)
				return new List<Asset>();

			Asset? exactMatch = map.Assets.Find(x => x.Names.Any(y => string.Equals(y, searchQuery.ToUpper().Trim(), StringComparison.Ordinal)));

			return exactMatch != null
				? new List<Asset> { exactMatch }
				: map.Assets.FindAll(x => x.TexturePaths.Any(y => y.Path.Contains($"\\{searchQuery}")));
		}

		public bool ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite)
		{
			bool success = true;

			Directory.CreateDirectory(Globals.PacksRootPath);
			using var archive = new ZipArchive(packFile.OpenReadStream(), ZipArchiveMode.Read);
			foreach (var entry in archive.Entries)
			{
				foreach (var pack in packIds.Select(packId => Globals.Packs!.Find(x => x.Id == packId)))
				{
					if (pack == null)
					{
						success = false;
						continue;
					}

					TextureMapping? textureMapping = Globals.TextureMappings!.Find(x => x.Id == pack.TextureMappingsId);
					if (textureMapping == null)
					{
						success = false;
						continue;
					}

					string entryPath = entry.FullName.Replace("/", "\\");

					bool isMcMeta = false;
					Asset? asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.Path == entryPath));
					if (asset == null)
					{
						asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.MCMeta && y.MCMetaPath == entryPath));
						if (asset == null)
						{
							success = false;
							Console.WriteLine($"Invalid file {entry.FullName}");
							continue;
						}
						isMcMeta = true;
					}
					Console.WriteLine($"Valid file {entry.FullName}");
					string path = Path.Combine(Globals.MasterAssetsRootPath, pack.Id.ToString(), "texture");
					Directory.CreateDirectory(path);

					string extractPath = !isMcMeta ? Path.Combine(path, $"{asset.Id}.png") : Path.Combine(path, $"{asset.Id}.png.mcmeta");

					if (File.Exists(extractPath) && !overwrite)
						continue;
					entry.ExtractToFile(extractPath, overwrite);
				}
			}
			return success;
		}

		public bool GeneratePacks(List<Guid> packIds)
		{
			bool success = true;
			foreach (Pack? pack in packIds.Select(id => Globals.Packs!.Find(x => x.Id == id)))
			{
				if (pack == null)
				{
					success = false;
					continue;
				}
				TextureMapping? textureMapping = Globals.TextureMappings!.Find(x => x.Id == pack.TextureMappingsId);
				if (textureMapping == null)
				{
					success = false;
					continue;
				}

				string sourceFilePath = Path.Combine(Globals.MasterAssetsRootPath, pack.Id.ToString());
				string destinationPackPath = Path.Combine(Globals.PacksRootPath, pack.Id.ToString());
				Parallel.ForEach(pack.Branches, branch =>
				{
					string dest = Path.Combine(destinationPackPath, branch.Id.ToString());

					foreach (string dir in Directory.GetDirectories(dest))
						Directory.Delete(dir, true);

					Parallel.ForEach(textureMapping.Assets.Where(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(branch.Version))), asset =>
					{
						string sourceFile = Path.Combine(sourceFilePath, "texture", $"{asset.Id}.png");
						Parallel.ForEach(asset.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)), tex =>
						{
							Utils.CopyFile(sourceFile, Path.Combine(dest, tex.Path), true);
							if (tex.MCMeta)
								Utils.CopyFile($"{sourceFile}.mcmeta", Path.Combine(dest, tex.MCMetaPath), true);
						});
					});

				});
			}
			return success;
		}
	}
}
