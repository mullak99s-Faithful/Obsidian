namespace Obsidian.SDK.Models.Tools
{
	public class MCAssets
	{
		public string Name { get; set; }
		public int Version { get; set; }
		public DateTime CreatedDate { get; set; }
		public MinecraftRelease Minecraft { get; set; }
		public List<string> Textures { get; set; }
	}
}
