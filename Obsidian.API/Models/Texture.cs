using Obsidian.SDK.Models;

namespace Obsidian.API.Models
{
	public class Texture
	{
		public Guid Id { get; set; }
		public bool HasMCMeta { get; set; }
		public MCVersion MCVersion { get; set; }
	}
}
