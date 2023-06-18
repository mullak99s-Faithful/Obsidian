using Obsidian.API.Repository;
using Obsidian.SDK;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Extensions;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Tools;
using System.IO.Compression;

namespace Obsidian.API.Logic
{
	public class AutoGenerationLogic : IAutoGenerationLogic
	{
		private readonly IPackRepository _packRepository;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IToolsLogic _toolsLogic;
		private readonly IPackValidationLogic _packValidationLogic;
		private readonly string GENERATION_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoGeneration");

		public AutoGenerationLogic(IPackRepository packRepository, ITextureMapRepository textureMapRepository, IToolsLogic toolsLogic, IPackValidationLogic packValidationLogic)
		{
			_packRepository = packRepository;
			_textureMapRepository = textureMapRepository;
			_toolsLogic = toolsLogic;
			_packValidationLogic = packValidationLogic;
		}

		public async Task<List<TextureAsset>> GenerateMissingMappings(Guid packId, MinecraftVersion version)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return new List<TextureAsset>();

			Task<ResponseModel<MCAssets>> getRefAssetsTask = _toolsLogic.GetMinecraftJavaAssets(version.GetEnumDescription(), true);
			Task<TextureMapping?> getMapTask = _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);

			await Task.WhenAll(getRefAssetsTask, getMapTask);

			TextureMapping? map = getMapTask.Result;
			ResponseModel<MCAssets> response = getRefAssetsTask.Result;
			if (map == null || !response.IsSuccess || response.Data == null)
				return new List<TextureAsset>();

			List<string> refAssets = response.Data.Textures;
			refAssets.Remove("pack.png");

			List<string> assetsForVersion = map.GetAssetsForVersion(version).Select(asset => asset.Replace("\\", "/")).ToList();

			PackReport report = await _packValidationLogic.CompareTextures(assetsForVersion, refAssets);
			Console.WriteLine($"Missing Textures: {string.Join("\n", report.MissingTextures)}");

			return report.MissingTextures.Select(missingTexture => new TextureAsset
			{
				Id = Guid.NewGuid(),
				Names = GenerateNamesForTexture(missingTexture),
				TexturePaths = new List<Texture>()
				{
					new()
					{
						Path = missingTexture.Replace("/", "\\"),
						MCVersion = new(version, null)
					}
				}
			}).ToList();
		}

		public async Task<List<TextureAsset>> GenerateOptifineMappings(Guid packId, MinecraftVersion version, IFormFile packZipFile)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return new List<TextureAsset>();

			Task<TextureMapping?> mapTask = _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);

			Guid zipId = Guid.NewGuid();
			string zipExtractPath = Path.Combine(GENERATION_PATH, zipId.ToString());
			Directory.CreateDirectory(zipExtractPath);

			string zipFilePath = Path.Combine(zipExtractPath, packZipFile.FileName);

			List<Task> tasks = new() { mapTask };
			await using (var fileStream = new FileStream(zipFilePath, FileMode.Create))
			{
				tasks.Add(packZipFile.CopyToAsync(fileStream));
				await Task.WhenAll(tasks);
			}
			ZipFile.ExtractToDirectory(zipFilePath, zipExtractPath);

			TextureMapping? map = mapTask.Result;

			string tempPath = Path.Combine(GENERATION_PATH, zipId.ToString());
			string optifinePath = Path.Combine(tempPath, "assets", "minecraft");

			if (Directory.Exists(Path.Combine(optifinePath, "optifine")))
				optifinePath = Path.Combine(optifinePath, "optifine", "ctm");
			else if (Directory.Exists(Path.Combine(optifinePath, "mcpatcher")))
				optifinePath = Path.Combine(optifinePath, "mcpatcher", "ctm");
			else
				return new List<TextureAsset>();

			string fixedOptifinePath = optifinePath.Replace(tempPath, "").TrimStart('\\', '/');

			List<string> optifineAssets = Utils.GetAllTextures(optifinePath).Select(x => Path.Combine(fixedOptifinePath, x.Replace("/", "\\"))).ToList();
			List<string> texMapAssets = map?.GetAssetsForVersion(version).Select(asset => asset.Replace("\\", "/")).ToList() ?? new List<string>();

			Task.Run(() => Utils.FastDeleteAll(tempPath)).ConfigureAwait(false).GetAwaiter();

			return optifineAssets.Where(x => !texMapAssets.Contains(x)).Select(texture => new TextureAsset
			{
				Id = Guid.NewGuid(),
				Names = GenerateNamesForTexture(texture),
				TexturePaths = CreateTextures(texture, version)
			}).ToList();
		}

		private List<Texture> CreateTextures(string texPath, MinecraftVersion minVersion)
		{
			List<Texture> textures = new();
			string newTexPath = texPath.Replace("/", "\\");
			if (newTexPath.Contains("ctm\\glass") && !newTexPath.Contains("tinted")) // Unique for some packs
			{
				textures.Add(new Texture()
				{
					Path = newTexPath.Replace("assets\\minecraft\\optifine", "assets\\minecraft\\mcpatcher"),
					MCVersion = new(MinecraftVersion.MC17, MinecraftVersion.MC112)
				});
				textures.Add(new Texture()
				{
					Path = newTexPath,
					MCVersion = new(MinecraftVersion.MC113, null)
				});
			}
			else
			{
				textures.Add(new Texture()
				{
					Path = newTexPath,
					MCVersion = new(minVersion, null)
				});
			}
			return textures;
		}

		private List<string> GenerateNamesForTexture(string texturePath)
		{
			texturePath = texturePath.Replace("\\", "/");
			string? fileName = Path.GetFileNameWithoutExtension(texturePath);
			if (string.IsNullOrWhiteSpace(fileName))
				return new List<string>();

			string finalFileName = fileName.ToUpper().Replace("_", " ");

			// Normal Textures
			if (texturePath.Contains("assets/minecraft/textures/trims/models/armor"))
			{
				return new List<string>()
				{
					$"{finalFileName} ARMOR TRIM",
					$"{finalFileName} ARMOUR TRIM"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/trims/models"))
			{
				return new List<string>()
				{
					$"{finalFileName} MODELS TRIM"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/trims/items"))
			{
				return new List<string>()
				{
					$"{finalFileName} ITEM"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/particle"))
			{
				return new List<string>()
				{
					$"PARTICLE {finalFileName}"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/painting"))
			{
				return new List<string>()
				{
					$"{finalFileName} PAINTING"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/gui/advancements"))
			{
				return new List<string>()
				{
					$"{finalFileName} ADVANCEMENTS"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/gui/container"))
			{
				return new List<string>()
				{
					$"{finalFileName} CONTAINER"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/gui/title"))
			{
				return new List<string>()
				{
					$"{finalFileName} TITLE"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/gui/hanging_signs"))
			{
				return new List<string>()
				{
					$"{finalFileName} HANGING SIGN GUI"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/gui"))
			{
				return new List<string>()
				{
					$"{finalFileName} GUI"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/item"))
			{
				return new List<string>()
				{
					finalFileName,
					$"ITEM {finalFileName}"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/block"))
			{
				return new List<string>()
				{
					finalFileName,
					$"BLOCK {finalFileName}"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/environment"))
			{
				return new List<string>()
				{
					finalFileName,
					$"ENVIRONMENT {finalFileName}"
				};
			}
			if (texturePath.Contains("assets/minecraft/textures/map"))
			{
				return new List<string>()
				{
					finalFileName,
					$"MAP {finalFileName}"
				};
			}

			// Realms
			if (texturePath.Contains("assets/realms/textures/gui/realms"))
			{
				return new List<string>()
				{
					$"REALMS {finalFileName}"
				};
			}

			// Optifine
			if (texturePath.Contains("assets/minecraft/optifine/ctm") || texturePath.Contains("assets/minecraft/mcpatcher/ctm"))
			{
				string? folderName = Path.GetFileName(Path.GetDirectoryName(texturePath))?.ToUpper().Replace("_", " ");
				string finalName = string.IsNullOrWhiteSpace(folderName) || folderName == "CTM" ? $"OPTIFINE CTM {finalFileName}" : $"OPTIFINE CTM {folderName} {finalFileName}";

				return new List<string>()
				{
					finalName
				};
			}

			return new List<string>()
			{
				finalFileName
			};
		}
	}

	public interface IAutoGenerationLogic
	{
		Task<List<TextureAsset>> GenerateMissingMappings(Guid packId, MinecraftVersion version);
		Task<List<TextureAsset>> GenerateOptifineMappings(Guid packId, MinecraftVersion version, IFormFile packZipFile);
	}
}
