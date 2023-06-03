using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;

namespace Obsidian.API.Logic
{
	public class MiscAssetLogic : IMiscAssetLogic
	{
		private readonly IPackRepository _packRepository;
		private readonly IMiscBucket _miscBucket;

		public MiscAssetLogic(IPackRepository packRepository, IMiscBucket miscBucket)
		{
			_packRepository = packRepository;
			_miscBucket = miscBucket;
		}

		public async Task AddMiscAsset(Pack pack, MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] zipBytes, bool overwrite = true)
		{
			MiscAsset miscAsset = new MiscAsset(minVersion, maxVersion, zipBytes);

			if (!pack.MiscAssetIds.Contains(miscAsset.Id))
				pack.MiscAssetIds.Add(miscAsset.Id);

			List<Task> tasks = new()
			{
				_packRepository.UpdatePackById(pack.Id, null, null, null, null, null, pack.MiscAssetIds, null, null),
				_miscBucket.UploadMisc(miscAsset, overwrite)
			};
			await Task.WhenAll(tasks);
		}

		public async Task DeleteMiscAsset(Pack pack, Guid assetId)
		{
			if (await _miscBucket.DoesMiscExist(assetId))
			{
				pack.MiscAssetIds.Remove(assetId);
				List<Task> tasks = new()
				{
					_miscBucket.DeleteMisc(assetId),
					RemoveMiscIdFromPack(pack, assetId)
				};
				await Task.WhenAll(tasks);
			}
			else if (pack.MiscAssetIds.Contains(assetId))
				await RemoveMiscIdFromPack(pack, assetId);
		}

		private async Task RemoveMiscIdFromPack(Pack pack, Guid assetId)
		{
			pack.MiscAssetIds.Remove(assetId);
			await _packRepository.UpdatePackById(pack.Id, null, null, null, null, null, pack.MiscAssetIds, null, null);
		}
	}

	public interface IMiscAssetLogic
	{
		Task AddMiscAsset(Pack pack, MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] zipBytes, bool overwrite = true);
		Task DeleteMiscAsset(Pack pack, Guid assetId);
	}
}
