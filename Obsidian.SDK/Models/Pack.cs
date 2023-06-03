using System.Text.Json;

namespace Obsidian.SDK.Models
{
	public class Pack
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<PackBranch> Branches { get; set; } = new();
		public Guid TextureMappingsId { get; set; }
		public Guid? ModelMappingsId { get; set; }
		public Guid? BlockStateMappingsId { get; set; }
		public List<Guid> MiscAssetIds { get; set; } = new();
		public bool EnableEmissives { get; set; }
		public string EmissiveSuffix { get; set; } = "_e";
		public string Description { get; set; }

		public Pack(string name, string description, Guid textureMappingsId, Guid? modelMappingsId, Guid? blockStateMappingsId, bool enableEmissives = false, string emissiveSuffix = "_e") : this()
		{
			Id = Guid.NewGuid();
			Name = name;
			Description = description;
			TextureMappingsId = textureMappingsId;
			ModelMappingsId = modelMappingsId;
			BlockStateMappingsId = blockStateMappingsId;
			EnableEmissives = enableEmissives;
			EmissiveSuffix = emissiveSuffix;
		}

		public Pack()
		{ }

		public string CreatePackMCMeta(PackBranch branch)
		{
			Minecraft.Pack pack = new(this, branch.Version);
			PackWrapper wrapper = new(pack);
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
