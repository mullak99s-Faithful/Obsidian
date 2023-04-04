using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Obsidian.SDK.Models
{
	public class Texture
	{
		public string Path { get; set; }

		[DefaultValue(false)]
		public bool MCMeta { get; set; }
		public MCVersion MCVersion { get; set; }

		[JsonIgnore]
		public string? MCMetaPath => MCMeta ? $"{Path}.mcmeta" : null;

		public override string ToString()
		{
			return MCMeta ? $"[{MCVersion}]: {Path} | ({MCMetaPath})" : $"[{MCVersion}]: {Path}";
		}
	}
}
