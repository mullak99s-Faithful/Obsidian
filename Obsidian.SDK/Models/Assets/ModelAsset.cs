using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Minecraft;

namespace Obsidian.SDK.Models.Assets
{
	public class ModelAsset
	{
		public Guid Id { get; set; }
		public List<string> Names { get; set; }
		public string Path { get; set; }
		public string FileName { get; set; }
		public BlockModel Model { get; set; }

		public ModelAsset(BlockModel rawModel, List<string> names, string path, string fileName, List<TextureAsset> textureAssets)
		{
			Id = Guid.NewGuid();
			Names = names;
			Path = path;
			FileName = fileName;
			Model = ConvertRawModelToModel(rawModel, textureAssets);
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
		private BlockModel ConvertRawModelToModel(BlockModel rawModel, List<TextureAsset> textureAssets)
		{
			Dictionary<string, string>? textures = rawModel.Textures;
			if (textures != null && textures.Any())
			{
				foreach (var texture in textures)
				{
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
					Texture tex = textureAssets.First(x => x.Id == Guid.Parse(texId)).TexturePaths
						.First(x => x.MCVersion.IsMatchingVersion(version));

					model.Textures[texture.Key] = ConvertInternalPathToModelTexPath(tex.Path.ToLower());
				}
			}
			return model.Serialize();
		}
	}
}
