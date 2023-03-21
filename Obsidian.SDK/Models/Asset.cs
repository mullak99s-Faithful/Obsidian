namespace Obsidian.SDK.Models
{
	public class Asset
	{
		public Guid Id { get; set; }
		public List<string> Names { get; set; }
		public List<Texture> TexturePaths { get; set; }

		public override string ToString()
		{
			return $"Id: {Id},\nNames: [\n{string.Join(",\n", Names)}\n],\nTexturePaths: [\n{string.Join(",\n", TexturePaths)}\n]";
		}
	}
}
