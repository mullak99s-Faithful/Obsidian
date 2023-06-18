namespace Obsidian.SDK.Models.Assets
{
	public class OptifineProperties
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string FileName { get; set; }
		public byte[] Data { get; set; }
		public MCVersion MCVersion { get; set; }
	}
}
