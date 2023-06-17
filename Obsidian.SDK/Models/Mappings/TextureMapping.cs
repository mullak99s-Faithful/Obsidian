using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;

namespace Obsidian.SDK.Models.Mappings
{
	public class TextureMapping
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<TextureAsset> Assets { get; set; }

		public List<string> GetAssetsForVersion(MinecraftVersion version)
		{
			List<string> assetsForVersion = new List<string>();
			foreach (TextureAsset asset in Assets)
			{
				if (asset.TexturePaths.Any(x => x.MCVersion.IsMatchingVersion(version)))
					assetsForVersion.AddRange(asset.GetTexturePathsForVersion(version));
			}
			return assetsForVersion;
		}
	}
}
