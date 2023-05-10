using Obsidian.SDK.Enums;

namespace Obsidian.SDK.Models.Assets
{
	public class BlockStateAsset
	{
		public Guid Id { get; set; }
		public List<BlockState> Blockstates { get; set; }

		public BlockStateAsset()
		{
			Id = Guid.NewGuid();
			Blockstates = new List<BlockState>();
		}

		public void AddBlockstate(string fileName, MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] data)
		{
			BlockState? existing = Blockstates.FirstOrDefault(x => x.MCVersion.IsMatchingVersion(minVersion) || x.MCVersion.IsMatchingVersion(maxVersion));
			if (existing != null)
			{
				Blockstates.Remove(existing);

				if (minVersion < existing.MCVersion.MinVersion)
					existing.MCVersion.MinVersion = minVersion;
				if (maxVersion > existing.MCVersion.MaxVersion)
					existing.MCVersion.MaxVersion = maxVersion;

				existing.Data = data;

				Blockstates.Add(existing);
			}
			else
			{
				Blockstates.Add(new BlockState()
				{
					FileName = fileName,
					Data = data,
					MCVersion = new MCVersion()
					{
						MinVersion = minVersion,
						MaxVersion = maxVersion
					}
				});
			}
		}

		public void RemoveBlockstate(string fileName)
			=> Blockstates.RemoveAll(x => x.FileName == fileName);
	}
}
