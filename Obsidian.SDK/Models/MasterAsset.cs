using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obsidian.SDK.Models
{
	public class MasterAsset
	{
		public Guid Id { get; set; }
		public List<string> Names { get; set; }
		public bool HasMCMeta { get; set; }
	}
}
