using Microsoft.VisualBasic.CompilerServices;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Extensions;

namespace Obsidian.SDK.Models
{
	public class PackBranch
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public MinecraftVersion Version { get; set; }

		public PackBranch(string name, MinecraftVersion version)
		{
			Id = Guid.NewGuid();
			Name = name;
			Version = version;
		}

		public string GetVersion()
			=> Version.GetEnumDescription();
	}
}
