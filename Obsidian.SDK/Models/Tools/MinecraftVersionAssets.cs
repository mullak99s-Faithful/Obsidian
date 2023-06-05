namespace Obsidian.SDK.Models.Tools
{
	public class MinecraftVersionAssets
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; }
		public int Version { get; set; }
		public string Edition { get; set; }
		public string JSON { get; set; }
	}
}
