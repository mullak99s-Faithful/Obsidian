using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Obsidian.API.Cache;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Minecraft;

namespace Obsidian.API.Repository
{
	public class ModelMapRepository : IModelMapRepository
	{
		private readonly IMongoCollection<ModelMapping> _collection;
		private readonly IModelMapCache _cache;

		public ModelMapRepository(IMongoDatabase database, IModelMapCache cache)
		{
			_collection = database.GetCollection<ModelMapping>("ModelMapping");
			_cache = cache;
		}

		#region Create

		public async Task AddModelMap(ModelMapping modelMapping)
			=> await _collection.InsertOneAsync(modelMapping);
		#endregion

		#region Read
		public async Task<ModelMapping?> GetModelMappingById(Guid id)
		{
			try
			{
				if (_cache.TryGetValue(id, out ModelMapping modelMap))
					return modelMap;

				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				var result = await _collection.Find(filter).FirstOrDefaultAsync();

				_cache.Set(id, result);
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<List<ModelMapping>> GetModelMappingsByName(string name)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Name, name);
				return await _collection.Find(filter).ToListAsync();
			}
			catch (Exception)
			{
				return new List<ModelMapping>(0);
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllModelMappingIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<ModelMapping>.Filter.Empty;
				var projection = Builders<ModelMapping>.Projection.Include(t => t.Id).Include(t => t.Name);
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

		public async Task<List<ModelMapping>> GetAllModelMappings()
		{
			try
			{
				var ids = await GetAllModelMappingIds();

				List<ModelMapping> mappings = new();
				foreach (var id in ids)
				{
					if (_cache.TryGetValue(id.Key, out ModelMapping modelMap))
						mappings.Add(modelMap);
				}

				if (mappings.Count == ids.Count)
					return mappings;

				var missingIds = ids.Where(x => !mappings.Select(y => y.Id).Contains(x.Key)).Select(x => x.Key).ToList();

				var filter = Builders<ModelMapping>.Filter.In(t => t.Id, missingIds);
				var documents = await _collection.Find(filter).ToListAsync();

				foreach (var document in documents.Where(x => x != null))
					_cache.Set(document.Id, document);

				mappings.AddRange(documents);
				return mappings;
			}
			catch (Exception)
			{
				return new List<ModelMapping>(0);
			}
		}

		public async Task<List<ModelAsset>> GetAllModelAssetsForVersion(MinecraftVersion version)
		{
			var filter = Builders<ModelMapping>.Filter.ElemMatch(x => x.Models, m => m.MCVersion.IsMatchingVersion(version));
			var projection = Builders<ModelMapping>.Projection.Include(x => x.Models);
			var cursor = await _collection.FindAsync(filter, new FindOptions<ModelMapping, BsonDocument> { Projection = projection });

			var models = new List<ModelAsset>();
			while (await cursor.MoveNextAsync())
			{
				var documents = cursor.Current;
				foreach (var document in documents)
				{
					var mappedModels = BsonSerializer.Deserialize<ModelMapping>(document).Models;
					models.AddRange(mappedModels.Where(m => m.MCVersion.IsMatchingVersion(version)));
				}
			}
			return models;
		}

		public async Task<ModelAsset?> GetModelAsset(Guid modelAssetId)
		{
			var filter = Builders<ModelMapping>.Filter.ElemMatch(x => x.Models, m => m.Id == modelAssetId);
			var projection = Builders<ModelMapping>.Projection.Include(x => x.Models);

			var cursor = await _collection.FindAsync(filter, new FindOptions<ModelMapping, BsonDocument> { Projection = projection });
			var result = await cursor.FirstOrDefaultAsync();

			if (result == null)
			{
				return null;
			}

			var model = result["Models"].AsBsonArray
				.Select(x => BsonSerializer.Deserialize<ModelAsset>(x.AsBsonDocument))
				.FirstOrDefault(x => x.Id == modelAssetId);

			return model;
		}

		public async Task<string> GetModelMappingNameById(Guid id)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
			var projection = Builders<ModelMapping>.Projection.Include(t => t.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}
		public async Task<bool> DoesModelExist(ModelAsset model, Guid modelMapId)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

				var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
				ModelAsset? existingModel = existingMap.Models.Find(x => (x.Id == model.Id || x.Name == model.Name) && x.MCVersion.DoesOverlap(model.MCVersion));
				return existingModel != null;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<ModelAsset?> GetExistingModel(ModelAsset model, Guid modelMapId)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

				var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
				ModelAsset? existingModel = existingMap.Models.Find(x => x.Id == model.Id || (x.FileName == model.FileName && x.Path == model.Path));
				return existingModel;
			}
			catch (Exception)
			{
				return null;
			}
		}
		#endregion

		#region Update
		public async Task<bool> UpdateNameById(Guid id, string newName)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				var update = Builders<ModelMapping>.Update.Set(t => t.Name, newName);
				var updated = await _collection.UpdateOneAsync(filter, update);

				if (updated.IsModifiedCountAvailable)
					_cache.Remove(id);

				return updated.IsModifiedCountAvailable;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> AddModel(ModelAsset asset, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();

			existingMap.Models.Add(asset);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsModifiedCountAvailable)
				_cache.Remove(modelMapId);

			Console.WriteLine($"Added model {asset}");

			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> EditModel(BlockModel asset, Guid modelId, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			ModelAsset? existingModel = existingMap.Models.Find(x => x.Id == modelId);
			if (existingModel == null)
				return false;

			existingModel.Update(asset);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsModifiedCountAvailable)
				_cache.Remove(modelMapId);

			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> DeleteModel(Guid modelId, Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			var model = existingMap.Models.Find(x => x.Id == modelId);
			if (model == null)
				return false;

			existingMap.Models.Remove(model);

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsModifiedCountAvailable)
				_cache.Remove(modelMapId);

			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> ClearModels(Guid modelMapId)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();

			existingMap.Models = new();

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsModifiedCountAvailable)
				_cache.Remove(modelMapId);

			return updated.IsModifiedCountAvailable;
		}

		public async Task<bool> ReplaceModels(Guid modelMapId, List<ModelAsset> modelAssets)
		{
			var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, modelMapId);

			var existingMap = await _collection.Find(filter).FirstOrDefaultAsync();
			if (existingMap == null)
				return false;

			foreach (var asset in modelAssets)
				asset.Model ??= existingMap.Models.Find(x => x.Id == asset.Id)?.Model;

			if (modelAssets.Any(x => x.Model == null))
			{
				Console.WriteLine("Some model assets had null data. Models were not replaced!");
				return false;
			}

			existingMap.Models = modelAssets;

			var update = Builders<ModelMapping>.Update.Set(t => t.Models, existingMap.Models);
			var updated = await _collection.UpdateOneAsync(filter, update);

			if (updated.IsModifiedCountAvailable)
				_cache.Remove(modelMapId);

			return updated.IsModifiedCountAvailable;
		}
		#endregion

		#region Delete
		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<ModelMapping>.Filter.Eq(t => t.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);
				return deleted.DeletedCount > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface IModelMapRepository
	{
		// Create
		Task AddModelMap(ModelMapping modelMapping);

		// Read
		Task<ModelMapping?> GetModelMappingById(Guid id);
		Task<List<ModelMapping>> GetModelMappingsByName(string name);
		Task<Dictionary<Guid, string>> GetAllModelMappingIds();
		Task<List<ModelMapping>> GetAllModelMappings();
		Task<List<ModelAsset>> GetAllModelAssetsForVersion(MinecraftVersion version);
		Task<ModelAsset?> GetModelAsset(Guid modelAssetId);
		Task<string> GetModelMappingNameById(Guid id);
		Task<bool> DoesModelExist(ModelAsset model, Guid modelMapId);
		Task<ModelAsset?> GetExistingModel(ModelAsset model, Guid modelMapId);

		// Update
		Task<bool> UpdateNameById(Guid id, string newName);
		Task<bool> AddModel(ModelAsset asset, Guid modelMapId);
		Task<bool> EditModel(BlockModel asset, Guid modelId, Guid modelMapId);
		Task<bool> DeleteModel(Guid modelId, Guid modelMapId);
		Task<bool> ClearModels(Guid modelMapId);
		Task<bool> ReplaceModels(Guid modelMapId, List<ModelAsset> modelAssets);

		// Delete
		Task<bool> DeleteById(Guid id);
	}
}
