using System.Text.Json;

namespace Obsidian.SDK.Models
{
	public class Pack
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<PackBranch> Branches { get; set; } = new();
		public Guid TextureMappingsId { get; set; }
		public string Description { get; set; }

		public Pack(string name, string description, Guid textureMappingsId) : this()
		{
			Id = Guid.NewGuid();
			Name = name;
			TextureMappingsId = textureMappingsId;
		}

		public Pack()
		{ }

		public string CreatePackMCMeta(PackBranch branch)
		{
			Minecraft.Pack pack = new Minecraft.Pack(this, branch.Version);
			PackWrapper wrapper = new PackWrapper(pack);
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				WriteIndented = true
			};
			return JsonSerializer.Serialize(wrapper, options);
		}
	}

	public class PackWrapper
	{
		public Minecraft.Pack pack { get; set; }

		public PackWrapper(Minecraft.Pack pack)
		{
			this.pack = pack;
		}
	}
}
