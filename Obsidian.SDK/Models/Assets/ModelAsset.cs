using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Minecraft;

namespace Obsidian.SDK.Models.Assets
{
	public class ModelAsset
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Path { get; set; }
		public string FileName { get; set; }
		public BlockModel? Model { get; set; }
		public MCVersion MCVersion { get; set; }

		public ModelAsset(BlockModel? rawModel, MCVersion version, string name, string path, string fileName, List<TextureAsset>? textureAssets)
		{
			Id = Guid.NewGuid();
			Name = name;
			Path = path;
			FileName = fileName;
			Model = ConvertRawModelToModel(rawModel, textureAssets);
			MCVersion = version;

			if (path.ToLower().Contains(".json"))
				throw new ArgumentException("Path cannot contain the filename");
		}

		public void Update(BlockModel model)
		{
			Model = model;
		}

		public void Update(BlockModel rawModel, List<TextureAsset> textureAssets)
		{
			Model = ConvertRawModelToModel(rawModel, textureAssets);
		}

		/// <summary>
		/// Convert the raw resourcepack block model into Obsidian's format (replaces texture paths with Asset Id's)
		/// </summary>
		/// <param name="rawModel">Raw MC Block model</param>
		/// <param name="textureAssets">Texture mappings</param>
		/// <returns>A BlockModel in Obsidian's format</returns>
		/// <exception cref="ArgumentNullException">No matching asset in texture mappings</exception>
		private BlockModel? ConvertRawModelToModel(BlockModel? rawModel, List<TextureAsset>? textureAssets)
		{
			if (rawModel == null || textureAssets == null)
				return null;

			Dictionary<string, string>? textures = rawModel.Textures;
			if (textures != null && textures.Any())
			{
				foreach (var texture in textures)
				{
					if (texture.Value.StartsWith('#'))
						continue; // Skip non-texture paths

					string texPath = ConvertModelTexPathToInternalPath(texture.Value.ToLower());
					try
					{
						// Replace the texture path with an Asset Id
						if (rawModel.Textures != null)
							rawModel.Textures[texture.Key] = textureAssets.First(x => x.TexturePaths.FindAll(y => y.Path == texPath).Any()).Id.ToString();
					}
					catch (ArgumentNullException)
					{
						Console.WriteLine($"No matching asset for {texture.Value} [{texture.Key}]! Expected path = {texPath}");
						throw new ArgumentNullException(texture.Key, $"The model uses a texture asset that is not defined in the current mappings! ({texture.Value} [{texture.Key}])");
					}
				}
			}
			return rawModel;
		}

		/// <summary>
		/// Convert the resourcepack-accepted path format to an internal path (full paths, and using backslashes)
		/// </summary>
		/// <param name="path">Accepted Resoucepack Format</param>
		/// <returns>Raw/Internal path</returns>
		private string ConvertModelTexPathToInternalPath(string path)
		{
			string fixPath = path.Replace("/", "\\");
			if (fixPath.Contains(":"))
			{
				string[] parts = fixPath.Split(':');
				return $"assets\\{parts[0]}\\textures\\{parts[1]}.png";
			}
			return $"assets\\minecraft\\textures\\{fixPath}.png";
		}

		/// <summary>
		/// Convert the internal path (full paths, and using backslashes) to a resourcepack-accepted path format
		/// </summary>
		/// <param name="path">Raw/Internal path</param>
		/// <returns>Accepted Resoucepack Format</returns>
		private string ConvertInternalPathToModelTexPath(string path)
		{
			if (path.StartsWith("assets\\minecraft\\textures"))
				return path.Replace("assets\\minecraft\\textures\\", "").Replace("\\", "/").Replace(".png", "");

			string[] parts = path.Split("\\textures\\");
			return $"{parts[0].Replace("assets\\", "")}:{parts[1].Replace("\\", "/").Replace(".png", "")}";
		}

		public string Serialize(List<TextureAsset> textureAssets, MinecraftVersion version)
		{
			BlockModel model = Model;
			if (model.Textures != null)
			{
				foreach (var texture in model.Textures)
				{
					string texId = texture.Value;

					if (!Guid.TryParse(texId, out Guid texGuid))
						continue; // Skip non-GUID texture paths

					TextureAsset? asset = textureAssets.FirstOrDefault(x => x.Id == texGuid);
					if (asset == null)
					{
						Console.WriteLine($"Model Serialize: Null asset! TexId = {texId}");
						continue;
					}


					// Matching version & ideally not an entity path
					Texture? tex = asset.TexturePaths.FirstOrDefault(x => x.MCVersion.IsMatchingVersion(version) && !x.Path.Replace("\\", "/").Contains("entity/")) ??
					               asset.TexturePaths.FirstOrDefault(x => x.MCVersion.IsMatchingVersion(version));

					if (tex == null)
					{
						Console.WriteLine($"Model Serialize: Null texture! Asset = {asset.Names.First()}, TexId = {texId}, Model = {FileName}");
						continue;
					}

					model.Textures[texture.Key] = ConvertInternalPathToModelTexPath(tex.Path.ToLower());
				}
			}
			return model.Serialize();
		}
	}
}
