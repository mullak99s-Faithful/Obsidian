using System.Globalization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Obsidian.API.Repository
{
	public class BlockstateBucket : IBlockstateBucket
	{
		private GridFSBucket _bucket;

		public BlockstateBucket(IMongoDatabase database)
		{
			_bucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "Blockstate",
				ChunkSizeBytes = 1024 * 1024,
				WriteConcern = WriteConcern.WMajority
			});
		}

		public async Task<bool> UploadBlockstate(Guid packId, string blockStateName, byte[] blockState, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeleteBlockstate(packId, blockStateName);
				else if (await DoesBlockstateExist(packId, blockStateName))
					return false;

				await _bucket.UploadFromBytesAsync(CreateBlockstateFileName(packId, blockStateName), blockState);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<byte[]?> DownloadBlockstate(Guid packId, string blockStateName)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, CreateBlockstateFileName(packId, blockStateName)))).FirstOrDefaultAsync();

				if (fileInfo != null)
					return await _bucket.DownloadAsBytesAsync(fileInfo.Id);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesBlockstateExist(Guid packId, string blockStateName)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, CreateBlockstateFileName(packId, blockStateName)))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteBlockstate(Guid packId, string blockStateName)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, CreateBlockstateFileName(packId, blockStateName)))).FirstOrDefaultAsync();
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

		private string CreateBlockstateFileName(Guid packId, string blockStateName)
			=> $"{packId}_{blockStateName.Replace(".json", "", true, CultureInfo.CurrentCulture)}.json";
	}

	public interface IBlockstateBucket
	{
		Task<bool> UploadBlockstate(Guid packId, string blockStateName, byte[] blockState, bool overwrite = false);
		Task<byte[]?> DownloadBlockstate(Guid packId, string blockStateName);
		Task<bool> DoesBlockstateExist(Guid packId, string blockStateName);
		Task<bool> DeleteBlockstate(Guid packId, string blockStateName);
	}
}
