using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Obsidian.API.Repository
{
	public class TextureBucket : ITextureBucket
	{
		private GridFSBucket _bucket;

		public TextureBucket(IMongoDatabase database)
		{
			_bucket = new GridFSBucket(database, new GridFSBucketOptions
			{
				BucketName = "Textures",
				ChunkSizeBytes = 1024 * 1024,
				WriteConcern = WriteConcern.WMajority
			});
		}

		public async Task<bool> UploadTexture(Guid packId, Guid textureId, byte[] texture, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeleteTexture(packId, textureId);
				else if (await DoesTextureExist(packId, textureId))
					return false;

				await _bucket.UploadFromBytesAsync($"{packId}_{textureId}.png", texture);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<byte[]?> DownloadTexture(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png"))).FirstOrDefaultAsync();

				if (fileInfo != null)
					return await _bucket.DownloadAsBytesAsync(fileInfo.Id);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesTextureExist(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png"))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteTexture(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png"))).FirstOrDefaultAsync();
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

		public async Task<bool> UploadMCMeta(Guid packId, Guid textureId, byte[] mcMeta, bool overwrite = false)
		{
			try
			{
				if (overwrite)
					await DeleteMCMeta(packId, textureId);
				else if (await DoesMCMetaExist(packId, textureId))
					return false;

				await _bucket.UploadFromBytesAsync($"{packId}_{textureId}.png.mcmeta", mcMeta);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<byte[]?> DownloadMCMeta(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png.mcmeta"))).FirstOrDefaultAsync();

				if (fileInfo != null)
					return await _bucket.DownloadAsBytesAsync(fileInfo.Id);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<bool> DoesMCMetaExist(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png.mcmeta"))).FirstOrDefaultAsync();
				return fileInfo != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteMCMeta(Guid packId, Guid textureId)
		{
			try
			{
				var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, $"{packId}_{textureId}.png.mcmeta"))).FirstOrDefaultAsync();
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

	public interface ITextureBucket
	{
		Task<bool> UploadTexture(Guid packId, Guid textureId, byte[] texture, bool overwrite = false);
		Task<byte[]?> DownloadTexture(Guid packId, Guid textureId);
		Task<bool> DoesTextureExist(Guid packId, Guid textureId);
		Task<bool> DeleteTexture(Guid packId, Guid textureId);

		Task<bool> UploadMCMeta(Guid packId, Guid textureId, byte[] mcMeta, bool overwrite = false);
		Task<byte[]?> DownloadMCMeta(Guid packId, Guid textureId);
		Task<bool> DoesMCMetaExist(Guid packId, Guid textureId);
		Task<bool> DeleteMCMeta(Guid packId, Guid textureId);
	}
}
