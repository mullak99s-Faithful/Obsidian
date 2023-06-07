using Obsidian.API.Repository;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Tools;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Obsidian.API.Logic
{
	public class ContinuousPackLogic : IContinuousPackLogic
	{
		private readonly string _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Continuous");

		private readonly ITextureBucket _textureBucket;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IMiscBucket _miscBucket;
		private readonly IPackPngLogic _packPngLogic;
		private readonly IToolsLogic _toolsLogic;

		// TODO: Move this to its own logic class?
		private readonly List<string> _textureBlacklist = new()
		{
			@"assets\/minecraft\/optifine",
			@"assets\/\b(?!minecraft\b|realms\b).*?\b",
			@"textures\/.+\/.+_e(missive)?\.png",
			@"assets\/minecraft\/optifine",
			@"textures\/misc",
			@"textures\/font",
			@"_MACOSX",
			@"assets\/minecraft\/textures\/ctm",
			@"assets\/minecraft\/textures\/custom",
			@"textures\/colormap",
			@"background\/panorama_overlay.png",
			@"assets\/minecraft\/textures\/environment\/clouds.png"
		};

		public ContinuousPackLogic(ITextureBucket textureBucket, ITextureMapRepository textureMapRepository, IMiscBucket miscBucket, IPackPngLogic packPngLogic, IToolsLogic toolsLogic)
		{
			_textureBucket = textureBucket;
			_textureMapRepository = textureMapRepository;
			_miscBucket = miscBucket;
			_packPngLogic = packPngLogic;
			_toolsLogic = toolsLogic;
		}

		public void AddPack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (!Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created pack {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(packPath);

				#if DEBUG
				File.Create(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}")).Dispose();
				#endif
			}
		}

		public void DeletePack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted pack {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(packPath));

				#if DEBUG
				if (File.Exists(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}")))
					File.Delete(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}"));
				#endif
			}
		}

		public void AddBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(branchPath);

				#if DEBUG
				File.Create(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}")).Dispose();
				#endif
			}
		}

		public void DeleteBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(branchPath));

				#if DEBUG
				if (File.Exists(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}")))
					File.Delete(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}"));
				#endif
			}
		}

		public async Task AddTexture(Pack pack, TextureAsset texture)
		{
			List<PackBranch> branches = pack.Branches;
			byte[]? textureBytes = await _textureBucket.DownloadTexture(pack.Id, texture.Id);
			byte[]? mcMetaBytes = null;
			if (textureBytes == null)
				return;

			List<string> texDestinationPaths = new();
			List<string> mcMetaDestinationPaths = new();

			foreach (PackBranch branch in branches)
			{
				string branchPath = GetBranchPath(pack, branch);
				foreach (var tex in texture.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
				{
					texDestinationPaths.Add(Path.Combine(branchPath, tex.Path));
					if (tex.MCMeta)
					{
						mcMetaBytes ??= await _textureBucket.DownloadMCMeta(pack.Id, texture.Id);
						mcMetaDestinationPaths.Add(Path.Combine(branchPath, tex.MCMetaPath));
					}
				}
			}

			List<Task> ioTasks = new();
			foreach (string destinationPath in texDestinationPaths)
			{
				string? dir = Path.GetDirectoryName(destinationPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				ioTasks.Add(WriteChangedFileBytes(destinationPath, textureBytes));
			}

			if (mcMetaBytes != null && mcMetaDestinationPaths.Count > 0)
			{
				foreach (string destinationPath in mcMetaDestinationPaths)
				{
					string? dir = Path.GetDirectoryName(destinationPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					ioTasks.Add(WriteChangedFileBytes(destinationPath, mcMetaBytes));
				}
			}
			await Task.WhenAll(ioTasks);
		}

		public async Task AddModel(Pack pack, ModelAsset asset, List<TextureAsset>? textureAssets = null)
		{
			if (textureAssets == null)
			{
				TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(pack.Id);
				if (textureMapping == null)
					return;

				textureAssets = textureMapping.Assets;
			}

			List<Task> ioTasks = new();
			foreach (PackBranch branch in pack.Branches)
			{
				string modelJson = asset.Serialize(textureAssets, branch.Version);
				if (!string.IsNullOrWhiteSpace(modelJson))
				{
					string dirPath = Path.Combine(GetMinecraftDirectory(pack, branch), "models", asset.Path);
					Directory.CreateDirectory(dirPath);

					string filePath = Path.Combine(dirPath, asset.FileName);
					ioTasks.Add(File.WriteAllTextAsync(filePath, modelJson, Encoding.UTF8));
				}
			}
			await Task.WhenAll(ioTasks);
		}

		public async Task AddBlockState(Pack pack, BlockState blockState)
		{
			if (blockState.Data.Length > 0)
			{
				List<Task> ioTasks = new();
				foreach (var dirPath in pack.Branches.Select(branch => Path.Combine(GetMinecraftDirectory(pack, branch), "blockstates")))
				{
					Directory.CreateDirectory(dirPath);

					string filePath = Path.Combine(dirPath, blockState.FileName);
					ioTasks.Add(File.WriteAllBytesAsync(filePath, blockState.Data));
				}
				await Task.WhenAll(ioTasks);
			}
		}

		public async Task AddMisc(Pack pack)
		{
			List<Task<MiscAsset?>> downloadTasks = pack.MiscAssetIds.Select(id => _miscBucket.DownloadMisc(id)).ToList();
			await Task.WhenAll(downloadTasks);

			List<Task> ioTasks = new();
			foreach (PackBranch branch in pack.Branches)
			{
				foreach (MiscAsset? asset in downloadTasks.Select(task => task.Result).Where(x => x != null && x.MCVersion.IsMatchingVersion(branch.Version)))
					ioTasks.Add(asset!.Extract(GetBranchPath(pack, branch)));
			}
			await Task.WhenAll(ioTasks);
		}

		public async Task PackAutomation(Pack pack)
		{
			Console.WriteLine($"Running automation for {pack.Name}");

			List<Task> automationTasks = new();
			foreach (PackBranch branch in pack.Branches)
			{
				string destination = GetBranchPath(pack, branch);
				automationTasks.Add(File.WriteAllTextAsync(Path.Combine(destination, "pack.mcmeta"), pack.CreatePackMCMeta(branch), Encoding.UTF8));  // pack.mcmeta

				// pack.png
				byte[]? packPng = await _packPngLogic.DownloadPackPng(pack.Id);
				if (packPng is { Length: > 0 })
					automationTasks.Add(File.WriteAllBytesAsync(Path.Combine(destination, "pack.png"), packPng));

				// Optifine
				string optifineDirectory = GetOptifineDirectory(pack, branch);
				Directory.CreateDirectory(optifineDirectory);

				if (pack.EnableEmissives)
					automationTasks.Add(File.WriteAllTextAsync(Path.Combine(optifineDirectory, "emissive.properties"), $"suffix.emissive={pack.EmissiveSuffix}"));
			}
			await Task.WhenAll(automationTasks);
		}

		public async Task PackValidation(Pack pack)
		{
			IEnumerable<Task> validationTasks = pack.Branches.Select(branch => BranchValidation(pack, branch));
			await Task.WhenAll(validationTasks);
		}

		private async Task BranchValidation(Pack pack, PackBranch branch)
		{
			var response = await _toolsLogic.GetMinecraftJavaAssets(branch.GetVersion(), true);

			if (!response.IsSuccess || response.Data == null)
			{
				Console.WriteLine($"Unable to run pack validation for {pack.Name} - {branch.Name}: {response.Message}");
				return;
			}

			Console.WriteLine($"Running pack validation for {pack.Name} - {branch.Name}");

			string branchPath = GetBranchPath(pack, branch);
			MCAssets assets = response.Data;
			List<string> packAssets = GetAllTextures(branchPath);
			string reportPath = Path.Combine(branchPath, $"Report-{pack.Name}-{branch.Name}.txt");

			PackReport packReport = await CompareTextures(packAssets, assets.Textures);

			Console.WriteLine($"Missing textures for {pack.Name} - {branch.Name}: {packReport.MissingTotal}");
			await packReport.GenerateReport(reportPath);
		}

		// TODO: Move this to its own logic class?
		private List<string> GetAllTextures(string path)
		{
			string[] pngFiles = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
			return pngFiles.Select(file => file.Replace("/", "\\").Replace(path, "").Replace("\\", "/").TrimStart('/')).ToList();
		}

		// TODO: Move this to its own logic class?
		private async Task<PackReport> CompareTextures(List<string> packFiles, List<string> refFiles)
		{
			PackReport packReport = new PackReport();

			// Run through all of the reference (MC) textures
			Task refTask = Task.Run(() =>
			{
				refFiles.ForEach(x =>
				{
					if (!_textureBlacklist.Any(rule => Regex.IsMatch(x, rule)))
					{
						packReport.TotalTextures++;
						if (packFiles.Contains(x))
							packReport.MatchingTextures.Add(x); // Pack contains this texture
						else
							packReport.MissingTextures.Add(x); // Pack doesn't contain this texture
					}
				});
			});

			// Run through all of the pack textures
			Task packTask = Task.Run(() =>
			{
				packFiles.ForEach(x =>
				{
					if (!_textureBlacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
						packReport.UnusedTextures.Add(x); // MC doesn't contain this texture
				});
			});
			await Task.WhenAll(refTask, packTask); // Run async to improve speed
			return packReport;
		}

		private async Task WriteChangedFileBytes(string path, byte[] bytes)
		{
			if (File.Exists(path))
			{
				try
				{
					using SHA1 sha1 = SHA1.Create();
					await using var fileStream = File.OpenRead(path);
					byte[] existingFileHash = await sha1.ComputeHashAsync(fileStream);
					byte[] newFileHash = sha1.ComputeHash(bytes);

					// Don't replace files if they are the same
					if (existingFileHash.SequenceEqual(newFileHash))
						return;
				}
				catch (Exception e)
				{
					Console.WriteLine($"[ContinuousPackLogic] Error: {e}");
				}
			}
			await File.WriteAllBytesAsync(path, bytes);
		}

		private string GetPackPath(Pack pack)
			=> Path.Combine(_rootPath, pack.Id.ToString());

		private string GetBranchPath(Pack pack, PackBranch branch)
			=> Path.Combine(GetPackPath(pack), branch.Id.ToString());

		private string GetMinecraftDirectory(Pack pack, PackBranch branch)
			=> Path.Combine(GetBranchPath(pack, branch), "assets", "minecraft");

		private string GetOptifineDirectory(Pack pack, PackBranch branch)
			=> Path.Combine(GetMinecraftDirectory(pack, branch), (int)branch.Version < 13 ? "mcpatcher" : "optifine");
	}

	public interface IContinuousPackLogic
	{
		void AddPack(Pack pack);
		void DeletePack(Pack pack);
		void AddBranch(Pack pack, PackBranch branch);
		void DeleteBranch(Pack pack, PackBranch branch);
		Task AddTexture(Pack pack, TextureAsset texture);
		Task AddModel(Pack pack, ModelAsset asset, List<TextureAsset>? textureAssets = null);
		Task AddBlockState(Pack pack, BlockState blockState);
		Task AddMisc(Pack pack);
		Task PackAutomation(Pack pack);
		Task PackValidation(Pack pack);
	}
}
