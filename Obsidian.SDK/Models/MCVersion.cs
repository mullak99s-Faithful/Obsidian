using Obsidian.SDK.Enums;
using Obsidian.SDK.Extensions;

namespace Obsidian.SDK.Models
{
	public class MCVersion
	{
		public MinecraftVersion MinVersion { get; set; }
		public MinecraftVersion MaxVersion { get; set; }

		public bool IsMatchingVersion(MinecraftVersion version)
			=> version >= MinVersion && version <= MaxVersion;

		public override string ToString()
		{
			if (MinVersion == MaxVersion || MinVersion != 0 && MaxVersion == 0)
				return $"{MinVersion.GetEnumDescription()}";
			if (MinVersion > 0 && MaxVersion > 0)
				return $"{MinVersion.GetEnumDescription()}-{MaxVersion.GetEnumDescription()}";
			return "N/A";
		}
	}
}
