using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Obsidian.API.Repository
{
	public class PackPngBucket : IPackPngBucket
	{
		private GridFSBucket _bucket;

		public PackPngBucket(IMongoDatabase database)
		{
			_bucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "PackPng",
				ChunkSizeBytes = 1024 * 1024,
				WriteConcern = WriteConcern.WMajority
			});
		}

		public async Task<bool> UploadPackPng(Guid packId, byte[] texture, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeletePackPng(packId);
				else if (await DoesPackPngExist(packId))
					return false;

				await _bucket.UploadFromBytesAsync($"{packId}.png", texture);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<byte[]?> DownloadPackPng(Guid packId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}.png"))).FirstOrDefaultAsync();

				if (fileInfo != null)
					return await _bucket.DownloadAsBytesAsync(fileInfo.Id);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesPackPngExist(Guid packId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}.png"))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeletePackPng(Guid packId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}.png"))).FirstOrDefaultAsync();
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

	public interface IPackPngBucket
	{
		Task<bool> UploadPackPng(Guid packId, byte[] texture, bool overwrite = false);
		Task<byte[]?> DownloadPackPng(Guid packId);
		Task<bool> DoesPackPngExist(Guid packId);
		Task<bool> DeletePackPng(Guid packId);
	}
}
