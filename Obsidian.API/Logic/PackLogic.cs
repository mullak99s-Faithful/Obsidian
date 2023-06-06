using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using System.IO.Compression;
using System.Text;
using Obsidian.SDK.Models.Tools;
using Pack = Obsidian.SDK.Models.Pack;
using System.Text.RegularExpressions;

namespace Obsidian.API.Logic
{
	public class PackLogic : IPackLogic
	{
		private readonly IPackPngLogic _packPngLogic;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IModelMapRepository _modelMapRepository;
		private readonly IBlockStateMapRepository _blockStateMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly ITextureBucket _textureBucket;
		private readonly IMiscBucket _miscBucket;
		private readonly IToolsLogic _toolsLogic;
		private readonly IContinuousPackLogic _continuousPackLogic;

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

		public PackLogic(IPackPngLogic packPngLogic, ITextureMapRepository textureMapRepository, IModelMapRepository modelMapRepository, IBlockStateMapRepository blockStateMapRepository, IPackRepository packRepository, ITextureBucket textureBucket, IMiscBucket miscBucket, IToolsLogic toolsLogic, IContinuousPackLogic continuousPackLogic)
		{
			_packPngLogic = packPngLogic;
			_textureMapRepository = textureMapRepository;
			_modelMapRepository = modelMapRepository;
			_blockStateMapRepository = blockStateMapRepository;
			_packRepository = packRepository;
			_textureBucket = textureBucket;
			_miscBucket = miscBucket;
			_toolsLogic = toolsLogic;
			_continuousPackLogic = continuousPackLogic;
		}

		public async Task<bool> AddPack(Pack pack)
		{
			bool success = await _packRepository.AddPack(pack);
			if (success)
				_continuousPackLogic.AddPack(pack);

			return success;
		}

		public async Task<bool> DeletePack(Pack pack)
		{
			bool success = await _packRepository.DeleteById(pack.Id);
			if (success)
				_continuousPackLogic.DeletePack(pack);

			return success;
		}

		public async Task<bool> AddBranch(Pack pack, PackBranch branch)
		{
			bool success = await _packRepository.AddBranch(pack.Id, branch);
			if (success)
				_continuousPackLogic.AddBranch(pack, branch);

			return success;
		}

		public async Task<bool> DeleteBranch(Pack pack, PackBranch branch)
		{
			bool success = await _packRepository.DeleteBranch(pack.Id, branch.Id);
			if (success)
				_continuousPackLogic.DeleteBranch(pack, branch);

			return success;
		}

		public async Task TriggerPackCheck(Guid packId, bool doFullCheck = false)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return;

			Console.WriteLine($"Starting pack check for {pack.Name}...");

			_continuousPackLogic.AddPack(pack);
			foreach(PackBranch branch in pack.Branches)
				_continuousPackLogic.AddBranch(pack, branch);

			// Full Check
			if (!doFullCheck)
			{
				Console.WriteLine($"Finished pack check for {pack.Name}!");
				return;
			}

			// Textures
			TextureMapping? texMap = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			if (texMap == null)
				return;

			List<TextureAsset> supportedAssets = new();
			foreach(PackBranch branch in pack.Branches)
				supportedAssets.AddRange(texMap.Assets.Where(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(branch.Version))));

			foreach (TextureAsset asset in supportedAssets)
				await _continuousPackLogic.AddTexture(pack, asset);

			// Models

			// Blockstates

			// Misc

			Console.WriteLine($"Finished full pack check for {pack.Name}!");
		}

		public async Task ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite)
		{
			var tempFilePath = Path.GetTempFileName();
			await using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
			{
				await packFile.CopyToAsync(fileStream);
				fileStream.Close();
			}

			Task.Run(async () =>
			{
				Console.WriteLine("Beginning pack import!");
				using var archive = new ZipArchive(new FileStream(tempFilePath, FileMode.Open), ZipArchiveMode.Read);

				foreach (var entry in archive.Entries)
				{
					foreach (Guid packId in packIds)
					{
						Pack? pack = await _packRepository.GetPackById(packId);
						if (pack == null)
							continue;

						TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
						if (textureMapping == null)
							continue;

						string entryPath = entry.FullName.Replace("/", "\\");
						bool isMcMeta = false;
						TextureAsset? asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(version) && y.Path == entryPath));
						if (asset == null)
						{
							asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(version) && y.MCMeta && y.MCMetaPath == entryPath));
							if (asset == null)
							{
								Console.WriteLine($"Invalid asset: {entryPath}");
								continue;
							}
							isMcMeta = true;
							Console.WriteLine($"Valid MCMeta asset: {entryPath}");
						}
						else Console.WriteLine($"Valid texture asset: {entryPath}");

						if (!isMcMeta)
							await _textureBucket.UploadTexture(packId, asset.Id, Utils.WriteEntryToByteArray(entry), overwrite);
						else
							await _textureBucket.UploadMCMeta(packId, asset.Id, Utils.WriteEntryToByteArray(entry), overwrite);
					}
				}
				archive.Dispose();

				File.Delete(tempFilePath);
				Console.WriteLine("Finished importing pack!");
			}).ConfigureAwait(false).GetAwaiter();
		}

		public void GeneratePacks(List<Guid> packIds)
		{
			Task.Run(async () =>
			{
				IEnumerable<Task> packTasks = packIds.Select(GeneratePack);
				await Task.WhenAll(packTasks);
				Console.WriteLine("Finished generating packs!");
			});
		}

		private async Task GeneratePack(Guid packId)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return;

			TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			if (textureMapping == null)
				return;

			ModelMapping? modelMapping = pack.ModelMappingsId != null
				? await _modelMapRepository.GetModelMappingById(pack.ModelMappingsId.Value)
				: null;
			// ModelMapping can be null. Simply means no custom models exist for the pack.

			BlockStateMapping? blockStateMapping = pack.BlockStateMappingsId != null
				? await _blockStateMapRepository.GetBlockStateMappingById(pack.BlockStateMappingsId.Value)
				: null;
			// BlockStateMapping can be null. Simply means no custom blockstates exist for the pack.

			string destinationPackPath = GetPackDestinationPath(pack);

			// Delete ZIPs
			Parallel.ForEach(Directory.GetFiles(destinationPackPath, "*.zip"), File.Delete);

			foreach (var branch in pack.Branches)
			{
				// TODO: Not running in parallel to avoid MongoDB limits. Cache maybe?
				await GenerateBranch(destinationPackPath, pack, branch, textureMapping, modelMapping, blockStateMapping);
			}
			Console.WriteLine($"Finished generating pack {pack.Name}!");
		}

		private async Task GenerateBranch(string packPath, Pack pack, PackBranch branch, TextureMapping textureMapping, ModelMapping? modelMapping, BlockStateMapping? blockStateMapping)
		{
			string dest = Path.Combine(packPath, branch.Name.Replace(" ", "_"));
			Directory.CreateDirectory(dest);
			foreach (string dir in Directory.GetDirectories(dest))
				Utils.FastDeleteAll(dir);

			Console.WriteLine($"Started generating branch {branch.Name} for {pack.Name}!");

			Task textureAssetTask = Task.Run(async () =>
			{
				foreach (var asset in textureMapping.Assets.Where(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(branch.Version))))
					await AddTexture(pack, branch, asset, dest);
			});

			Task modelAssetTask = Task.Run(async () =>
			{
				if (modelMapping != null)
				{
					foreach (var asset in modelMapping.Models.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
						await AddModel(branch, textureMapping, asset, dest);
				}
			});

			Task blockStateTask = Task.Run(async () =>
			{
				if (blockStateMapping != null)
				{
					foreach (var asset in blockStateMapping.Models.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
						await AddBlockState(asset, dest);
				}
			});

			Task miscAssetsTask = Task.Run(async () =>
			{
				await AddMisc(pack, branch, dest);
			});

			Task automatedPackTask = Task.Run(async () =>
			{
				await PackAutomation(pack, branch, dest);
			});

			// These can be ran simultaneously since it won't exceed connections
			await Task.WhenAll(textureAssetTask, modelAssetTask, blockStateTask, miscAssetsTask, automatedPackTask);

			// TODO: Allow changing this at some point. Temporary name atm for testing.
			string zipPath = Path.Combine(packPath, $"{pack.Name}-{branch.Name}-Obsidian.zip");

			Task zipTask = Task.Run(() => Utils.CreateZipArchive(dest, zipPath));
			Task packValidationTask = PackValidation(pack, branch, dest, packPath);

			await Task.WhenAll(zipTask, packValidationTask);

			Console.WriteLine($"Finished generating branch {branch.Name} for {pack.Name}!");
		}

		private async Task AddTexture(Pack pack, PackBranch branch, TextureAsset asset, string destination)
		{
			byte[]? sourceFile = await _textureBucket.DownloadTexture(pack.Id, asset.Id);
			if (sourceFile != null)
			{
				foreach (var tex in asset.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
				{
					string textureDestination = Path.Combine(destination, tex.Path);

					string? texDirPath = Path.GetDirectoryName(textureDestination);
					if (string.IsNullOrEmpty(texDirPath))
						continue; // Shouldn't ever happen, logging just in-case?

					Directory.CreateDirectory(texDirPath);

					await File.WriteAllBytesAsync(textureDestination, sourceFile);
					if (tex.MCMeta)
					{
						byte[]? mcMetaFile = await _textureBucket.DownloadMCMeta(pack.Id, asset.Id);
						if (mcMetaFile != null)
							await File.WriteAllBytesAsync(Path.Combine(destination, tex.MCMetaPath), mcMetaFile);
					}
				}
			}
		}

		private async Task AddModel(PackBranch branch, TextureMapping textureMapping, ModelAsset asset, string destination)
		{
			string modelJson = asset.Serialize(textureMapping.Assets, branch.Version);

			if (!string.IsNullOrWhiteSpace(modelJson))
			{
				string dirPath = Path.Combine(GetMinecraftDirectory(destination), "models", asset.Path);
				Directory.CreateDirectory(dirPath);

				string filePath = Path.Combine(dirPath, asset.FileName);
				await File.WriteAllTextAsync(filePath, modelJson, Encoding.UTF8);
			}
		}

		private async Task AddBlockState(BlockState blockState, string destination)
		{
			if (blockState.Data.Length > 0)
			{
				string dirPath = Path.Combine(GetMinecraftDirectory(destination), "blockstates");
				Directory.CreateDirectory(dirPath);

				string filePath = Path.Combine(dirPath, blockState.FileName);
				await File.WriteAllBytesAsync(filePath, blockState.Data);
			}
		}

		private async Task AddMisc(Pack pack, PackBranch branch, string destination)
		{
			List<Task<MiscAsset?>> downloadTasks = pack.MiscAssetIds.Select(id => _miscBucket.DownloadMisc(id)).ToList();
			await Task.WhenAll(downloadTasks);

			foreach (MiscAsset? asset in downloadTasks.Select(task => task.Result).Where(x => x != null && x.MCVersion.IsMatchingVersion(branch.Version)))
				await asset!.Extract(destination);

		}

		private async Task PackAutomation(Pack pack, PackBranch branch, string destination)
		{
			Console.WriteLine($"Running automation for {pack.Name} - {branch.Name}");

			List<Task> automationTasks = new()
			{
				File.WriteAllTextAsync(Path.Combine(destination, "pack.mcmeta"), pack.CreatePackMCMeta(branch), Encoding.UTF8) // pack.mcmeta
			};

			// pack.png
			byte[]? packPng = await _packPngLogic.DownloadPackPng(pack.Id);
			if (packPng is { Length: > 0 })
				automationTasks.Add(File.WriteAllBytesAsync(Path.Combine(destination, "pack.png"), packPng));

			// Optifine
			string optifineDirectory = GetOptifineDirectory(branch, destination);
			Directory.CreateDirectory(optifineDirectory);

			if (pack.EnableEmissives)
				automationTasks.Add(File.WriteAllTextAsync(Path.Combine(optifineDirectory, "emissive.properties"), $"suffix.emissive={pack.EmissiveSuffix}"));

			await Task.WhenAll(automationTasks);
		}

		private async Task PackValidation(Pack pack, PackBranch branch, string packPath, string reportRootPath)
		{
			var response = await _toolsLogic.GetMinecraftJavaAssets(branch.GetVersion(), true);

			if (!response.IsSuccess || response.Data == null)
			{
				Console.WriteLine($"Unable to run pack validation for {pack.Name} - {branch.Name}: {response.Message}");
				return;
			}

			Console.WriteLine($"Running pack validation for {pack.Name} - {branch.Name}");

			MCAssets assets = response.Data;
			List<string> packAssets = GetAllTextures(packPath);
			string reportPath = Path.Combine(reportRootPath, $"Report-{pack.Name}-{branch.Name}.txt");

			PackReport packReport = await CompareTextures(packAssets, assets.Textures);

			Console.WriteLine($"Missing textures for {pack.Name} - {branch.Name}: {packReport.MissingTotal}");
			await packReport.GenerateReport(reportPath);
		}

		private List<string> GetAllTextures(string path)
		{
			string[] pngFiles = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
			return pngFiles.Select(file => file.Replace("/", "\\").Replace(path, "").Replace("\\", "/").TrimStart('/')).ToList();
		}

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

		private string GetOptifineDirectory(PackBranch branch, string destination)
			=> Path.Combine(GetMinecraftDirectory(destination), (int)branch.Version < 13 ? "mcpatcher" : "optifine");

		private string GetMinecraftDirectory(string destination)
			=> Path.Combine(destination, "assets", "minecraft");

		private string GetPackDestinationPath(Pack pack)
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPacks", pack.Name.Replace(" ", "_"));
			Directory.CreateDirectory(path);
			return path;
		}
	}

	public interface IPackLogic
	{
		Task<bool> AddPack(Pack pack);
		Task<bool> DeletePack(Pack pack);
		Task<bool> AddBranch(Pack pack, PackBranch branch);
		Task<bool> DeleteBranch(Pack pack, PackBranch branch);
		Task ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite);
		void GeneratePacks(List<Guid> packIds);
		Task TriggerPackCheck(Guid packId, bool doFullCheck = false);
	}
}
