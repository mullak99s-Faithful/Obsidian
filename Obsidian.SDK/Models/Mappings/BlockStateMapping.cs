namespace Obsidian.SDK.Models.Mappings
{
	public class BlockStateMapping
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public List<BlockState> Models { get; set; }

		public BlockStateMapping(string name)
		{
			Id = Guid.NewGuid();
			Name = name;
			Models = new List<BlockState>();
		}
	}
}
