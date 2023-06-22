namespace Obsidian.SDK.Models
{
	public class TextureMetadata
	{
		public string? Credit { get; set; }

		public string GetCredit()
		{
			return Credit ?? "Unknown";
		}
	}
}
