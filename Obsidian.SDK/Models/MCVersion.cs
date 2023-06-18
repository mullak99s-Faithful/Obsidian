using Obsidian.SDK.Enums;
using Obsidian.SDK.Extensions;

namespace Obsidian.SDK.Models
{
	public class MCVersion
	{
		public MinecraftVersion MinVersion { get; set; }
		public MinecraftVersion? MaxVersion { get; set; }

		public MCVersion(MinecraftVersion minVersion, MinecraftVersion? maxVersion)
		{
			MinVersion = minVersion;
			MaxVersion = maxVersion;
		}

		public MCVersion()
		{ }

		/// <summary>
		/// Check if a specific version matches this MCVersion
		/// </summary>
		/// <param name="version">Specified Minecraft Version</param>
		/// <returns>If the version matches</returns>
		public bool IsMatchingVersion(MinecraftVersion? version)
		{
			if (MaxVersion == 0) // MaxVersion should not be 0
				MaxVersion = null;

			int ver = (int?)version ?? int.MaxValue;
			int maxVersion = (int?)MaxVersion ?? int.MaxValue;
			return ver >= (int)MinVersion && ver <= maxVersion;
		}

		public bool DoesOverlap(MCVersion version)
			=> IsMatchingVersion(version.MinVersion) || IsMatchingVersion(version.MaxVersion);

		public override string ToString()
		{
			if (MinVersion == MaxVersion || MinVersion != 0 && MaxVersion == 0)
				return $"{MinVersion.GetEnumDescription()}";
			return MinVersion switch
			{
				> 0 when MaxVersion is null => $"{MinVersion.GetEnumDescription()}+",
				> 0 when MaxVersion > 0 => $"{MinVersion.GetEnumDescription()}-{MaxVersion.GetEnumDescription()}",
				_ => "N/A"
			};
		}
	}
}
