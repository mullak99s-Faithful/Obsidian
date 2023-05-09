using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Mappings;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Obsidian.SDK.Models.Assets;

namespace Obsidian.API.Logic
{
	public class PackLogic : IPackLogic
	{
		private ITextureLogic _texLogic;

		private ITextureMapRepository _textureMapRepository;
		private IModelMapRepository _modelMapRepository;
		private IPackRepository _packRepository;
		private ITextureBucket _textureBucket;

		public PackLogic(ITextureLogic texLogic, ITextureMapRepository textureMapRepository, IModelMapRepository modelMapRepository, IPackRepository packRepository, ITextureBucket textureBucket)
		{
			_texLogic = texLogic;
			_textureMapRepository = textureMapRepository;
			_modelMapRepository = modelMapRepository;
			_packRepository = packRepository;
			_textureBucket = textureBucket;
		}

		public void ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite)
		{
			Task.Run(async () =>
			{
				using var archive = new ZipArchive(packFile.OpenReadStream(), ZipArchiveMode.Read);
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
								continue;
							isMcMeta = true;
						}

						if (!isMcMeta)
							await _textureBucket.UploadTexture(packId, asset.Id, Utils.WriteEntryToByteArray(entry), overwrite);
						else
							await _textureBucket.UploadMCMeta(packId, asset.Id, Utils.WriteEntryToByteArray(entry), overwrite);
					}
				}
				Console.WriteLine("Finished importing pack!");
			});
		}

		public void GeneratePacks(List<Guid> packIds)
		{
			Task.Run(async () =>
			{
				foreach (Guid packId in packIds)
				{
					Pack? pack = _packRepository.GetPackById(packId).GetAwaiter().GetResult();
					if (pack == null)
						continue;

					TextureMapping? textureMapping = _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId).GetAwaiter().GetResult();
					if (textureMapping == null)
						continue;

					string destinationPackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPacks", pack.Name.Replace(" ", "_"));
					Directory.CreateDirectory(destinationPackPath);

					foreach (var branch in pack.Branches)
					{
						string dest = Path.Combine(destinationPackPath, branch.Name.Replace(" ", "_"));
						Directory.CreateDirectory(dest);
						foreach (string dir in Directory.GetDirectories(dest))
							Utils.FastDeleteAll(dir);

						Console.WriteLine($"Started generating branch {branch.Name} for {pack.Name}!");

						foreach (var asset in textureMapping.Assets.Where(x => x.TexturePaths.Any(y => y.MCVersion.IsMatchingVersion(branch.Version))))
						{
							byte[]? sourceFile = _textureBucket.DownloadTexture(packId, asset.Id).GetAwaiter().GetResult();
							if (sourceFile != null)
							{
								foreach (var tex in asset.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
								{
									string textureDestination = Path.Combine(dest, tex.Path);
									Directory.CreateDirectory(Path.GetDirectoryName(textureDestination));

									await File.WriteAllBytesAsync(textureDestination, sourceFile);
									if (tex.MCMeta)
									{
										byte[]? mcMetaFile = _textureBucket.DownloadMCMeta(packId, asset.Id).GetAwaiter().GetResult();
										if (mcMetaFile != null)
											await File.WriteAllBytesAsync(Path.Combine(dest, tex.MCMetaPath), mcMetaFile);
									}
								}
							}
						}

						await File.WriteAllTextAsync(Path.Combine(dest, "pack.mcmeta"), pack.CreatePackMCMeta(branch), Encoding.UTF8);

						// TODO: Create pack.png

						string zipPath = Path.Combine(destinationPackPath, $"{pack.Name}-{branch.Name}-Obsidian.zip");
						Utils.CreateZipArchive(dest, zipPath);

						Console.WriteLine($"Finished generating branch {branch.Name} for {pack.Name}!");
					}
					Console.WriteLine($"Finished generating pack {pack.Name}!");
				}
				Console.WriteLine("Finished generating packs!");
			});
		}
	}

	public interface IPackLogic
	{
		public void ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite);
		public void GeneratePacks(List<Guid> packIds);
	}
}
