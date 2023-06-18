using Obsidian.SDK.Enums;

namespace Obsidian.SDK.Models.Assets
{
	public class OptifineAsset
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; }
		public string Path { get; set; }
		public List<OptifineProperties> Properties { get; set; }

		public string GetPath(string packRootPath, MinecraftVersion version)
			=> System.IO.Path.Combine(packRootPath, "assets", "minecraft", (int)version > 12 ? "optifine" : "mcpatcher", "ctm", Path);
	}
}
