namespace Obsidian.SDK.Models.Assets
{
	public class OptifineAsset
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public List<string> Names { get; set; }
		public string Path { get; set; }
		public MCVersion MCVersion { get; set; }

		public string GetPath(string packRootPath)
			=> System.IO.Path.Combine(packRootPath, "assets", "minecraft", "optifine", "ctm", Path);
	}
}
