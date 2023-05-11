﻿using Obsidian.SDK.Enums;
using Obsidian.SDK.Extensions;

namespace Obsidian.SDK.Models
{
	public class MCVersion
	{
		public MinecraftVersion MinVersion { get; set; }
		public MinecraftVersion MaxVersion { get; set; }

		public MCVersion(MinecraftVersion minVersion, MinecraftVersion maxVersion)
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
		public bool IsMatchingVersion(MinecraftVersion version)
			=> version >= MinVersion && version <= MaxVersion;

		public bool DoesOverlap(MCVersion version)
			=> IsMatchingVersion(version.MinVersion) || IsMatchingVersion(version.MaxVersion);

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
