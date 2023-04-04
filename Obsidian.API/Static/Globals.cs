using System.Linq;
using System.Text.Json;
using Obsidian.SDK.Models;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Static
{
	public static class Globals
	{
		public static string PacksRootPath { get; set; }
		public static string TextureMappingsRootPath { get; set; }
		public static string MasterAssetsRootPath { get; set; }

		private static bool _isInitialized;

		public static List<Pack>? Packs { get; set; }
		public static List<TextureMapping>? TextureMappings { get; set; }
		public static List<MasterAsset>? MasterAssets { get; set; }

		public static void Init()
		{
			if (!_isInitialized)
			{
				PacksRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packs");
				TextureMappingsRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "texturemappings");
				MasterAssetsRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "masterassets");

				Directory.CreateDirectory(PacksRootPath);
				Directory.CreateDirectory(TextureMappingsRootPath);
				Directory.CreateDirectory(MasterAssetsRootPath);
				_isInitialized = true;

				LoadPacks();
				LoadTextureMappings();
			}
		}
		public static void LoadPacks()
		{
			string[] packs = Directory.GetDirectories(Globals.PacksRootPath);
			Packs = new List<Pack>();
			foreach (string pack in packs)
			{
				string jsonPath = Path.Combine(pack, "pack.json");
				if (File.Exists(jsonPath))
				{
					try
					{
						string rawJson = File.ReadAllText(jsonPath);
						Pack? packObj = JsonSerializer.Deserialize<Pack>(rawJson);
						if (packObj != null)
							Globals.Packs.Add(packObj);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}
		}

		public static async Task SavePacks()
		{
			List<Task> saveTasks = Packs!.Select(SavePack).ToList();
			await Task.WhenAll(saveTasks);

			// Delete undefined packs
			foreach (string? directory in Directory.GetDirectories(PacksRootPath).Select(x => Packs!.Find(y => y.Id.ToString() != Path.GetDirectoryName(x))).Select(z => z?.Id.ToString()))
			{
				if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) continue;
				Directory.Delete(directory, true);
			}
		}

		public static async Task SavePack(Pack pack)
		{
			string packPath = Path.Combine(PacksRootPath, pack.Id.ToString());
			Directory.CreateDirectory(packPath);

			foreach (PackBranch branch in pack.Branches)
			{
				string branchPath = Path.Combine(packPath, branch.Id.ToString());
				Directory.CreateDirectory(branchPath);
				await File.WriteAllTextAsync(Path.Combine(branchPath, "pack.mcmeta"), pack.CreatePackMCMeta(branch));
			}

			// Delete undefined branches
			foreach (string directory in Directory.GetDirectories(packPath).Where(x => pack.Branches.Select(y => y.Id).ToString() != Path.GetDirectoryName(x)))
				Directory.Delete(directory, true);

			string json = JsonSerializer.Serialize(pack);

			string jsonPath = Path.Combine(packPath, "pack.json");
			File.Delete(jsonPath);
			await File.WriteAllTextAsync(jsonPath, json);
		}

		public static void LoadTextureMappings()
		{
			string[] jsons = Directory.GetFiles(Globals.TextureMappingsRootPath);
			Globals.TextureMappings = new List<TextureMapping>();
			foreach (string map in jsons)
			{
				if (File.Exists(map))
				{
					try
					{
						string rawJson = File.ReadAllText(map);
						TextureMapping? mapObj = JsonSerializer.Deserialize<TextureMapping>(rawJson);
						if (mapObj != null)
							Globals.TextureMappings.Add(mapObj);
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}
		}

		public static async Task SaveTextureMaps()
		{
			List<Task> saveTasks = Globals.TextureMappings!.Select(SaveTextureMap).ToList();
			await Task.WhenAll(saveTasks);
		}

		public static async Task SaveTextureMap(TextureMapping map)
		{
			string json = JsonSerializer.Serialize(map);
			string jsonPath = Path.Combine(Globals.TextureMappingsRootPath, $"{map.Id}.json");
			File.Delete(jsonPath);
			await File.WriteAllTextAsync(jsonPath, json);
		}
	}
}
