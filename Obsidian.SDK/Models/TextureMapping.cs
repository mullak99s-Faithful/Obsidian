using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obsidian.SDK.Models
{
	public class TextureMapping
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<Asset> Assets { get; set; }
	}
}
