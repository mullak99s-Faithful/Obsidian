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
		public async Task<bool> AddOrUpdate(Guid mapId, Guid assetId, TextureMetadata metadata)
		{
			var existingAsset = await GetTexMeta(mapId, assetId);
			if (existingAsset == null)
			{
				TexMeta texMeta = new()
				{
					AssetId = assetId,
					MapId = mapId,
					Metadata = metadata
				};
				await _collection.InsertOneAsync(texMeta);
				return true;
			}
			return await UpdateTexMeta(existingAsset, metadata);
		}
		#endregion

		#region Read
		public async Task<TextureMetadata?> GetMetadata(Guid mapId, Guid assetId)
			=> (await GetTexMeta(mapId, assetId))?.Metadata;

		private async Task<TexMeta?> GetTexMeta(Guid mapId, Guid assetId)
		{
			try
			{
				var filterBuilder = Builders<TexMeta>.Filter;
				var filter = filterBuilder.And(
				filterBuilder.Eq(t => t.AssetId, mapId),
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
			var result = await _collection.UpdateOneAsync(filter: t => t.AssetId == meta.AssetId && t.MapId == meta.MapId,
				update: Builders<TexMeta>.Update.Set(t => t.Metadata, newMeta)
			);

			return result.IsModifiedCountAvailable;
		}
		#endregion

		#region Delete
		public async Task<bool> DeleteMetadata(Guid mapId, Guid assetId)
		{
			try
			{
				var filterBuilder = Builders<TexMeta>.Filter;
				var filter = filterBuilder.And(
					filterBuilder.Eq(t => t.AssetId, mapId),
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

		public async Task<bool> DeleteAllMetadata(Guid? mapId = null)
		{
			try
			{
				if (mapId != null)
				{
					var filterBuilder = Builders<TexMeta>.Filter;
					var filter = filterBuilder.And(
						filterBuilder.Eq(t => t.AssetId, mapId)
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
		Task<bool> AddOrUpdate(Guid mapId, Guid assetId, TextureMetadata metadata);

		// Read
		Task<TextureMetadata?> GetMetadata(Guid mapId, Guid assetId);

		// Delete
		Task<bool> DeleteMetadata(Guid mapId, Guid assetId);
		Task<bool> DeleteAllMetadata(Guid? mapId = null);
	}

	internal class TexMeta
	{
		public Guid MapId { get; set; }
		public Guid AssetId { get; set; }
		public TextureMetadata? Metadata { get; set; }
	}
}
