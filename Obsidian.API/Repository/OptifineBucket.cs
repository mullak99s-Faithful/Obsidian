using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Obsidian.API.Repository
{
	public class OptifineBucket : IOptifineBucket
	{
		private GridFSBucket _bucket;

		public OptifineBucket(IMongoDatabase database)
		{
			_bucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "OptifineAssetBucket",
				ChunkSizeBytes = 1024 * 1024,
				WriteConcern = WriteConcern.WMajority
			});
		}

		public async Task<bool> UploadOptifineAsset(Guid assetId, byte[] optifineZipAsset, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeleteOptifineAsset(assetId);
				else if (await DoesOptifineAssetExist( assetId))
					return false;

				await _bucket.UploadFromBytesAsync($"{assetId}.zip", optifineZipAsset);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<byte[]?> DownloadOptifineAsset(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}.zip"))).FirstOrDefaultAsync();

				if (fileInfo != null)
					return await _bucket.DownloadAsBytesAsync(fileInfo.Id);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesOptifineAssetExist(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}.zip"))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteOptifineAsset(Guid assetId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{assetId}.zip"))).FirstOrDefaultAsync();
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

	public interface IOptifineBucket
	{
		Task<bool> UploadOptifineAsset(Guid assetId, byte[] optifineZipAsset, bool overwrite = false);
		Task<byte[]?> DownloadOptifineAsset(Guid assetId);
		Task<bool> DoesOptifineAssetExist(Guid assetId);
		Task<bool> DeleteOptifineAsset(Guid assetId);
	}
}
