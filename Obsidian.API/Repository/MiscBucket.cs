using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Newtonsoft.Json;
using Obsidian.SDK.Models.Assets;
using System.Text;

namespace Obsidian.API.Repository
{
	public class MiscBucket : IMiscBucket
	{
		private GridFSBucket _bucket;

		public MiscBucket(IMongoDatabase database)
		{
			_bucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "Misc",
				ChunkSizeBytes = 1024 * 1024,
				WriteConcern = WriteConcern.WMajority
			});
		}

		public async Task<bool> UploadMisc(MiscAsset asset, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeleteMisc(asset.Id);
				else if (await DoesMiscExist(asset.Id))
					return false;

				byte[] assetRaw = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(asset));
				await _bucket.UploadFromBytesAsync($"{asset.Id}", assetRaw);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<MiscAsset?> DownloadMisc(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}"))).FirstOrDefaultAsync();
				if (fileInfo != null)
				{
					byte[] assetRaw = await _bucket.DownloadAsBytesAsync(fileInfo.Id);
					string assetJson = Encoding.UTF8.GetString(assetRaw);

					return JsonConvert.DeserializeObject<MiscAsset>(assetJson);
				}
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesMiscExist(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}"))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteMisc(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}"))).FirstOrDefaultAsync();
				if (fileInfo == null)
					return false;

				await _bucket.DeleteAsync(fileInfo.Id);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public interface IMiscBucket
	{
		Task<bool> UploadMisc(MiscAsset asset, bool overwrite = false);
		Task<MiscAsset?> DownloadMisc(Guid assetId);
		Task<bool> DoesMiscExist(Guid assetId);
		Task<bool> DeleteMisc(Guid assetId);
	}
}
