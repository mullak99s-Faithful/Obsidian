using System.ComponentModel;

namespace Obsidian.SDK.Models
{
	public class Texture
	{
		public string Path { get; set; }

		[DefaultValue(null)]
		public string? MCMeta { get; set; }
		public MCVersion MCVersion { get; set; }

		public override string ToString()
		{
			return MCMeta != null ? $"[{MCVersion}]: {Path} | ({MCMeta})" : $"[{MCVersion}]: {Path}";
		}
	}
}
