using Obsidian.SDK.Enums;

namespace Obsidian.SDK.Models.Minecraft
{
	public class Pack
	{
		public int pack_format { get; set; }
		public string description { get; set; }

		public Pack(Models.Pack pack, MinecraftVersion version)
		{
			description = pack.Description;
			pack_format = version switch
			{
				MinecraftVersion.MC17 => 1,
				MinecraftVersion.MC18 => 1,
				MinecraftVersion.MC19 => 2,
				MinecraftVersion.MC110 => 2,
				MinecraftVersion.MC111 => 3,
				MinecraftVersion.MC112 => 3,
				MinecraftVersion.MC113 => 4,
				MinecraftVersion.MC114 => 4,
				MinecraftVersion.MC115 => 5,
				MinecraftVersion.MC116 => 6,
				MinecraftVersion.MC117 => 7,
				MinecraftVersion.MC118 => 8,
				MinecraftVersion.MC119 => 9,
				MinecraftVersion.MC1193 => 12,
				MinecraftVersion.MC1194 => 13,
				_ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported version.")
			};
		}
	}
}
