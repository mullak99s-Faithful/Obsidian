using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Mappings;
using System.IO.Compression;
using System.Text;
using Obsidian.SDK.Models.Assets;
using Pack = Obsidian.SDK.Models.Pack;
using static System.Net.Mime.MediaTypeNames;

namespace Obsidian.API.Logic
{
	public class PackLogic : IPackLogic
	{
		private readonly ITextureLogic _texLogic;
		private readonly IPackPngLogic _packPngLogic;

		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IModelMapRepository _modelMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly ITextureBucket _textureBucket;

		public PackLogic(ITextureLogic texLogic, IPackPngLogic packPngLogic, ITextureMapRepository textureMapRepository, IModelMapRepository modelMapRepository, IPackRepository packRepository, ITextureBucket textureBucket)
		{
			_texLogic = texLogic;
			_packPngLogic = packPngLogic;
			_textureMapRepository = textureMapRepository;
			_modelMapRepository = modelMapRepository;
			_packRepository = packRepository;
			_textureBucket = textureBucket;
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
						TextureAsset? asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.Path == entryPath));
						if (asset == null)
						{
							asset = textureMapping.Assets.Find(x => x.TexturePaths.Any(y => y.MCMeta && y.MCMetaPath == entryPath));
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

			string destinationPackPath = GetPackDestinationPath(pack);
			foreach (var branch in pack.Branches)
			{
				// TODO: Not running in parallel to avoid MongoDB limits. Cache maybe?
				await GenerateBranch(destinationPackPath, pack, branch, textureMapping, modelMapping);
			}
			Console.WriteLine($"Finished generating pack {pack.Name}!");
		}

		private async Task GenerateBranch(string packPath, Pack pack, PackBranch branch, TextureMapping textureMapping, ModelMapping? modelMapping)
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
						await AddModel(pack, branch, asset, dest);
				}
			});

			Task packMcMetaTask = File.WriteAllTextAsync(Path.Combine(dest, "pack.mcmeta"), pack.CreatePackMCMeta(branch), Encoding.UTF8);
			Task packPngTask = AddPackPng(pack.Id, dest);

			// These can be ran simultaneously since it won't exceed connections
			await Task.WhenAll(textureAssetTask, modelAssetTask, packMcMetaTask, packPngTask);

			// TODO: Allow changing this at some point. Temporary name atm for testing.
			string zipPath = Path.Combine(packPath, $"{pack.Name}-{branch.Name}-Obsidian.zip");
			Utils.CreateZipArchive(dest, zipPath);

			Console.WriteLine($"Finished generating branch {branch.Name} for {pack.Name}!");
		}

		private async Task AddPackPng(Guid packId, string destination)
		{
			byte[]? packPng = await _packPngLogic.DownloadPackPng(packId);
			if (packPng == null || packPng.Length == 0)
				return;

			await File.WriteAllBytesAsync(Path.Combine(destination, "pack.png"), packPng);
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

		private async Task AddModel(Pack pack, PackBranch branch, ModelAsset asset, string destination)
		{
			// TODO
			await Task.CompletedTask;
		}

		private string GetPackDestinationPath(Pack pack)
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPacks", pack.Name.Replace(" ", "_"));
			Directory.CreateDirectory(path);
			return path;
		}
	}

	public interface IPackLogic
	{
		Task ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite);
		void GeneratePacks(List<Guid> packIds);
	}
}
