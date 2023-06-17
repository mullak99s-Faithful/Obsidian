using Obsidian.SDK.Enums;

namespace Obsidian.SDK.Models.Assets
{
	public class TextureAsset
	{
		public Guid Id { get; set; }
		public List<string> Names { get; set; }
		public List<Texture> TexturePaths { get; set; }

		public void Update(TextureAsset asset)
		{
			Names = asset.Names;
			TexturePaths = asset.TexturePaths;
		}

		public List<string> GetTexturePathsForVersion(MinecraftVersion version)
		{
			List<string> texturePathsForVersion = new List<string>();
			foreach (Texture texture in TexturePaths)
			{
				if (texture.MCVersion.IsMatchingVersion(version))
					texturePathsForVersion.Add(texture.Path);
			}
			return texturePathsForVersion;
		}

		public override string ToString()
			=> $"Id: {Id},\nNames: [\n{string.Join(",\n", Names)}\n],\nTexturePaths: [\n{string.Join(",\n", TexturePaths)}\n]";
	}
}
