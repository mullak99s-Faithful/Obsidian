using Obsidian.SDK.Models.Assets;

namespace Obsidian.SDK.Models.Mappings
{
	public class ModelMapping
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<ModelAsset> Models { get; set; }
	}
}
