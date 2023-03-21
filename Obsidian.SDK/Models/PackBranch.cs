using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obsidian.SDK.Enums;

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
	}
}
