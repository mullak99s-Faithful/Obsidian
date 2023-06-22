using MongoDB.Bson;
using MongoDB.Driver;
using Obsidian.SDK.Models;

namespace Obsidian.API.Repository
{
	public class TextureMetadataRepository : ITextureMetadataRepository
	{
		private readonly IMongoCollection<TexMeta> _collection;

		public TextureMetadataRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<TexMeta>("TextureMetadata");
		}

		#region Create
		public async Task<bool> AddOrUpdate(Guid packId, Guid assetId, TextureMetadata metadata)
		{
			var existingAsset = await GetTexMeta(packId, assetId);
			if (existingAsset == null)
			{
				TexMeta texMeta = new()
				{
					AssetId = assetId,
					PackId = packId,
					Metadata = metadata
				};
				await _collection.InsertOneAsync(texMeta);
				return true;
			}
			return await UpdateTexMeta(existingAsset, metadata);
		}
		#endregion

		#region Read
		public async Task<TextureMetadata?> GetMetadata(Guid packId, Guid assetId)
			=> (await GetTexMeta(packId, assetId))?.Metadata;

		private async Task<TexMeta?> GetTexMeta(Guid packId, Guid assetId)
		{
			try
			{
				var filterBuilder = Builders<TexMeta>.Filter;
				var filter = filterBuilder.And(
				filterBuilder.Eq(t => t.PackId, packId),
					filterBuilder.Eq(t => t.AssetId, assetId)
				);

				var result = await _collection.Find(filter).FirstOrDefaultAsync();
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}
		#endregion

		#region Update

		private async Task<bool> UpdateTexMeta(TexMeta meta, TextureMetadata newMeta)
		{
			var result = await _collection.UpdateOneAsync(filter: t => t.AssetId == meta.AssetId && t.PackId == meta.PackId,
				update: Builders<TexMeta>.Update.Set(t => t.Metadata, newMeta)
			);

			return result.IsModifiedCountAvailable;
		}
		#endregion

		#region Delete
		public async Task<bool> DeleteMetadata(Guid packId, Guid assetId)
		{
			try
			{
				var filterBuilder = Builders<TexMeta>.Filter;
				var filter = filterBuilder.And(
					filterBuilder.Eq(t => t.AssetId, packId),
					filterBuilder.Eq(t => t.AssetId, assetId)
				);

				var result = await _collection.DeleteOneAsync(filter);
				return result.DeletedCount > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteAllMetadata(Guid? packId = null)
		{
			try
			{
				if (packId != null)
				{
					var filterBuilder = Builders<TexMeta>.Filter;
					var filter = filterBuilder.And(
						filterBuilder.Eq(t => t.AssetId, packId)
					);

					var result = await _collection.DeleteManyAsync(filter);
					return result.DeletedCount > 0;
				}
				else
				{
					var result = await _collection.DeleteManyAsync(_ => true);
					return result.DeletedCount > 0;
				}

			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface ITextureMetadataRepository
	{
		// Create
		Task<bool> AddOrUpdate(Guid packId, Guid assetId, TextureMetadata metadata);

		// Read
		Task<TextureMetadata?> GetMetadata(Guid packId, Guid assetId);

		// Delete
		Task<bool> DeleteMetadata(Guid packId, Guid assetId);
		Task<bool> DeleteAllMetadata(Guid? packId = null);
	}

	internal class TexMeta
	{
		public ObjectId Id { get; set; }
		public Guid PackId { get; set; }
		public Guid AssetId { get; set; }
		public TextureMetadata? Metadata { get; set; }
	}
}
