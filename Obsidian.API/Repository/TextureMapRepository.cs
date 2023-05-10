using MongoDB.Driver;
using Obsidian.API.Cache;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;

namespace Obsidian.API.Repository
{
	public class TextureMapRepository : ITextureMapRepository
	{
		private readonly IMongoCollection<TextureMapping> _collection;
		private readonly ITextureMapCache _cache;

		public TextureMapRepository(IMongoDatabase database, ITextureMapCache cache)
		{
			_collection = database.GetCollection<TextureMapping>("TextureMapping");
			_cache = cache;
		}

		#region Create
		public async Task AddTextureMap(TextureMapping textureMapping)
			=> await _collection.InsertOneAsync(textureMapping);
		#endregion

		#region Read
		public async Task<TextureMapping?> GetTextureMappingById(Guid id)
		{
			try
			{
				if (_cache.TryGetValue(id, out TextureMapping textureMap))
					return textureMap;

				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				var result = await _collection.Find(filter).FirstOrDefaultAsync();

				_cache.Set(id, result);
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllTextureMappingIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<TextureMapping>.Filter.Empty;
				var projection = Builders<TextureMapping>.Projection.Include(t => t.Id).Include(t => t.Name);
				var cursor = await _collection.Find(filter).Project(projection).ToCursorAsync();

				while (await cursor.MoveNextAsync())
				{
					foreach (var document in cursor.Current)
						dictionary.Add(document["_id"].AsGuid, document["Name"].AsString);
				}
				return dictionary;
			}
			catch (Exception)
			{
				return dictionary;
			}
		}

		public async Task<List<TextureMapping>> GetAllTextureMappings()
		{
			try
			{
				var ids = await GetAllTextureMappingIds();

				List<TextureMapping> mappings = new();
				foreach (var id in ids)
				{
					if (_cache.TryGetValue(id.Key, out TextureMapping textureMap))
						mappings.Add(textureMap);
				}

				if (mappings.Count == ids.Count)
					return mappings;

				var missingIds = ids.Where(x => !mappings.Select(y => y.Id).Contains(x.Key)).Select(x => x.Key).ToList();

				var filter = Builders<TextureMapping>.Filter.In(t => t.Id, missingIds);
				var documents = await _collection.Find(filter).ToListAsync();

				foreach(var document in documents.Where(x => x != null))
					_cache.Set(document.Id, document);

				mappings.AddRange(documents);
				return mappings;
			}
			catch (Exception)
			{
				return new List<TextureMapping>(0);
			}
		}

		public async Task<string> GetTextureMappingNameById(Guid id)
		{
			var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
			var projection = Builders<TextureMapping>.Projection.Include(t => t.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}
		#endregion

		#region Update
		public async Task<bool> UpdateNameById(Guid id, string newName)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				var update = Builders<TextureMapping>.Update.Set(t => t.Name, newName);
				var updated = await _collection.UpdateOneAsync(filter, update);

				if (updated.IsAcknowledged)
					_cache.Remove(id);

				return updated.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> AddTexture(TextureAsset asset, Guid textureMapId)
		{
			var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, textureMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			existingMap.Assets.Add(asset);

			var update = Builders<TextureMapping>.Update.Set(t => t.Assets, existingMap.Assets);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsAcknowledged)
				_cache.Remove(textureMapId);

			return updated.IsAcknowledged;
		}

		public async Task<bool> EditTexture(TextureAsset asset, Guid modelId, Guid textureMapId)
		{
			var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, textureMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			TextureAsset? existingTexture = existingMap.Assets.Find(x => x.Id == modelId);
			if (existingTexture == null)
				return false;

			existingTexture.Update(asset);

			var update = Builders<TextureMapping>.Update.Set(t => t.Assets, existingMap.Assets);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsAcknowledged)
				_cache.Remove(textureMapId);

			return updated.IsAcknowledged;
		}

		public async Task<bool> DeleteTexture(Guid modelId, Guid textureMapId)
		{
			var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, textureMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			var assets = existingMap.Assets.Find(x => x.Id == modelId);
			if (assets == null)
				return false;

			existingMap.Assets.Remove(assets);

			var update = Builders<TextureMapping>.Update.Set(t => t.Assets, existingMap.Assets);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsAcknowledged)
				_cache.Remove(textureMapId);

			return updated.IsAcknowledged;
		}
		#endregion

		#region Delete
		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);

				if (deleted.IsAcknowledged)
					_cache.Remove(id);

				return deleted.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface ITextureMapRepository
	{
		// Create
		Task AddTextureMap(TextureMapping textureMapping);

		// Read
		Task<TextureMapping?> GetTextureMappingById(Guid id);
		Task<Dictionary<Guid, string>> GetAllTextureMappingIds();
		Task<List<TextureMapping>> GetAllTextureMappings();
		Task<string> GetTextureMappingNameById(Guid id);

		// Update
		Task<bool> UpdateNameById(Guid id, string newName);
		Task<bool> AddTexture(TextureAsset asset, Guid textureMapId);
		Task<bool> EditTexture(TextureAsset asset, Guid modelId, Guid textureMapId);
		Task<bool> DeleteTexture(Guid modelId, Guid textureMapId);

		// Delete
		Task<bool> DeleteById(Guid id);
	}
}
