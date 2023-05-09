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

		public override string ToString()
			=> $"Id: {Id},\nNames: [\n{string.Join(",\n", Names)}\n],\nTexturePaths: [\n{string.Join(",\n", TexturePaths)}\n]";
	}
}
