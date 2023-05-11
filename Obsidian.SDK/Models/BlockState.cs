namespace Obsidian.SDK.Models
{
	public class BlockState
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string FileName { get; set; }
		public byte[] Data { get; set; }
		public MCVersion MCVersion { get; set; }

		public void Update(BlockState blockState)
		{
			Name = blockState.Name;
			FileName = blockState.FileName;
			Data = blockState.Data;
			MCVersion = blockState.MCVersion;
		}
	}
}
