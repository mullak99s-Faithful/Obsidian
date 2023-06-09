﻿using Obsidian.API.Extensions;
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
		private readonly IContinuousPackLogic _continuousPackLogic;
		private readonly IPackLogic _packLogic;

		public MiscAssetLogic(IPackRepository packRepository, IMiscBucket miscBucket, IContinuousPackLogic continuousPackLogic, IPackLogic packLogic)
		{
			_packRepository = packRepository;
			_miscBucket = miscBucket;
			_continuousPackLogic = continuousPackLogic;
			_packLogic = packLogic;
		}

		public async Task AddMiscAsset(Pack pack, string name, MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] zipBytes, bool overwrite = true)
		{
			MiscAsset miscAsset = new MiscAsset(name, minVersion, maxVersion, zipBytes);

			if (!pack.MiscAssetIds.Contains(miscAsset.Id))
				pack.MiscAssetIds.Add(miscAsset.Id);

			List<Task> tasks = new()
			{
				_packRepository.UpdatePackById(pack.Id, null, null, null, null, null, pack.MiscAssetIds, null, null, null),
				_miscBucket.UploadMisc(miscAsset, overwrite)
			};
			await Task.WhenAll(tasks);

			IEnumerable<PackBranch> matchingBranches = pack.Branches.Where(x => miscAsset.MCVersion.IsMatchingVersion(x.Version));

			List<Task> addMiscTasks = new();
			addMiscTasks.AddRange(matchingBranches.Select(x => _continuousPackLogic.AddMisc(pack, x)));
			Task.WhenAll(addMiscTasks).ConfigureAwait(false).GetAwaiter();
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
				_packLogic.TriggerPackCheck(pack.Id, true).ConfigureAwait(false).GetAwaiter();
			}
			else if (pack.MiscAssetIds.Contains(assetId))
				await RemoveMiscIdFromPack(pack, assetId);
		}

		public async Task<Dictionary<Guid, string>> GetMiscAssetNamesForPack(Pack pack)
		{
			List<Guid> miscAssetIds = pack.MiscAssetIds;

			List<Task<MiscAsset?>> downloadTasks = miscAssetIds.Select(assetId => _miscBucket.DownloadMisc(assetId)).ToList();
			await downloadTasks.WhenAllThrottledAsync(5);

			IEnumerable<MiscAsset?> miscAssets = downloadTasks.Select(task => task.Result);
			return miscAssets.Where(asset => asset != null).ToDictionary(asset => asset!.Id, asset => asset!.Name);
		}

		private async Task RemoveMiscIdFromPack(Pack pack, Guid assetId)
		{
			pack.MiscAssetIds.Remove(assetId);
			await _packRepository.UpdatePackById(pack.Id, null, null, null, null, null, pack.MiscAssetIds, null, null, null);
		}
	}

	public interface IMiscAssetLogic
	{
		Task AddMiscAsset(Pack pack, string name, MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] zipBytes, bool overwrite = true);
		Task DeleteMiscAsset(Pack pack, Guid assetId);
		Task<Dictionary<Guid, string>> GetMiscAssetNamesForPack(Pack pack);
	}
}
