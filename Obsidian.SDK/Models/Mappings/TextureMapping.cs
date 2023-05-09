using Obsidian.SDK.Models.Assets;

namespace Obsidian.SDK.Models.Mappings
{
	public class TextureMapping
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<TextureAsset> Assets { get; set; }
	}
}
